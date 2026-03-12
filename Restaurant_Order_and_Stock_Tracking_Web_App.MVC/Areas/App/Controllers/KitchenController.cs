// ============================================================================
//  Areas/App/Controllers/KitchenController.cs  —  KDS v5
//
//  SPRINT C (korundu):
//  [SC-3] [Area("App")] attribute → /App/Kitchen/Display 404 çözüldü
//  [SC-4] AppBaseController miras ALMIYOR — KDS [AllowAnonymous] gerektirir
//
//  DÜZELTME FAZI 1:
//  [SEC-01] ros-tenant cookie → Secure = !env.IsDevelopment()
//  [SEC-03] UpdateStatus → ros-tenant cookie tenantId çapraz doğrulaması
//  [BIZ-01] UpdateStatus → Trial süresi dolmuş tenant Forbid() ile reddedilir
//
//  DÜZELTME FAZI 2:
//  [ASYNC-03] Display() → KDS boş sipariş durumunda cookie hâlâ yazılır.
//             TenantId kaynağı öncelik sırası:
//               1. Açık sipariş listesinden    (runtime bilgi, en güvenilir)
//               2. ?tenantId= query parametresi (KDS URL'ine eklenmeli)
//             Cookie her iki durumda da yazılır; SignalR grup join'i garantilenir.
//  [ASYNC-04] UpdateStatus() → "Ready → Served" otomatik atlama kaldırıldı.
//             Aşçı "Hazır" bastığında item OrderItemStatus.Ready olur.
//             "Served" geçişi ayrı bir adım (garson servis eder).
//             State machine: Pending → Preparing → Ready → (garson) → Served
// ============================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Shared.Common;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers
{
    [Area("App")]
    [AllowAnonymous]                // KDS ekranı giriş gerektirmez
    public class KitchenController : Controller
    {
        private readonly RestaurantDbContext _db;
        private readonly IHubContext<RestaurantHub> _hub;
        private readonly IWebHostEnvironment _env;   // [SEC-01]

        public KitchenController(
            RestaurantDbContext db,
            IHubContext<RestaurantHub> hub,
            IWebHostEnvironment env)
        {
            _db = db;
            _hub = hub;
            _env = env;
        }

        // ── GET /App/Kitchen/Display?tenantId={slug} ──────────────────────────
        //
        // [ASYNC-03] tenantId route parametresi eklendi.
        // KDS TV ekranı URL'i artık şu formatta olmalı:
        //   /App/Kitchen/Display?tenantId=benim-restoranim
        //
        // Bu sayede mutfakta hiç açık sipariş olmasa bile (sabah açılışı,
        // servis arası) ros-tenant cookie YAZILIR ve SignalR grubuna JOIN
        // garantiyle gerçekleşir. İlk sipariş KDS'e anlık düşer.
        public async Task<IActionResult> Display(string? tenantId = null)
        {
            // ── [ASYNC-03] TenantId Kaynağı Belirleme ────────────────────────
            //
            // Öncelik 1: Açık siparişlerden — runtime'da en güncel bilgi.
            //   Avantaj: Tenant'ın gerçekten aktif olduğunu örtük olarak kanıtlar.
            //
            // Öncelik 2: ?tenantId= query parametresi — açık sipariş yokken devreye girer.
            //   Güvenlik notu: Bu değer aşağıda DB'den doğrulanmıyor (Display yalnızca
            //   okuma yapar; yazma işlemi UpdateStatus'ta doğrulanıyor). Bununla
            //   birlikte cookie değeri RestaurantHub.OnConnectedAsync'te DB'den
            //   doğrulanacağı için manipüle edilmiş bir değer SignalR grubuna giremez.

            var orders = await _db.Orders
                .Where(o => o.OrderStatus == OrderStatus.Open)
                .Include(o => o.Table)
                .Include(o => o.OrderItems
                    .Where(oi => oi.OrderItemStatus == OrderItemStatus.Pending
                              || oi.OrderItemStatus == OrderItemStatus.Preparing))
                    .ThenInclude(oi => oi.MenuItem)
                .OrderBy(o => o.OrderOpenedAt)
                .ToListAsync();

            orders = orders.Where(o => o.OrderItems.Any()).ToList();

            // ── [ASYNC-03] TenantId kaynağı belirleme ────────────────────────
            // Öncelik 1: Açık siparişler var → ilk siparişin TenantId'si güvenilir
            var resolvedTenantId = orders.FirstOrDefault()?.TenantId;

            // Öncelik 2: Açık sipariş yok, ama query string'de tenantId var
            if (string.IsNullOrEmpty(resolvedTenantId)
                && !string.IsNullOrEmpty(tenantId))
            {
                resolvedTenantId = tenantId;
            }

            // ── [KDS-COOKIE] ros-tenant cookie'yi HER HALÜKARDA yaz ──────────
            //
            // [ASYNC-03 FIX] Önceki implementasyon: yalnızca açık sipariş varsa yaz.
            //   Sorun: Boş mutfakta cookie yazılmıyor → SignalR gruba join yok →
            //          ilk gelen sipariş KDS'e düşmüyor.
            //
            // Yeni implementasyon: resolvedTenantId null değilse her zaman yaz.
            //   RestaurantHub.OnConnectedAsync DB'den doğrulayacağı için
            //   sahte bir tenantId cookie olsa bile SignalR gruba giremez.
            if (!string.IsNullOrEmpty(resolvedTenantId))
            {
                Response.Cookies.Append("ros-tenant", resolvedTenantId, new CookieOptions
                {
                    HttpOnly = false,                    // JS ile okunabilsin (SignalR client)
                    Secure = !_env.IsDevelopment(),   // [SEC-01] Production'da HTTPS zorunlu
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                });
            }

            return View(orders);
        }

        // ── POST /App/Kitchen/UpdateStatus ────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] KdsStatusUpdateDto dto)
        {
            if (dto is null)
                return BadRequest(new { message = "Geçersiz istek gövdesi." });

            // ── [SEC-03 / BIZ-01] Adım 1: TenantId Kimlik Doğrulaması ────────
            var tenantId = Request.Cookies["ros-tenant"];
            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized(new { message = "KDS kimliği doğrulanamadı. Sayfayı yenileyin." });

            // ── [SEC-03 / BIZ-01] Adım 2: Tenant DB Doğrulaması ─────────────
            var tenant = await _db.Tenants
                .AsNoTracking()
                .Select(t => new { t.TenantId, t.PlanType, t.TrialEndsAt, t.IsActive })
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

            if (tenant is null || !tenant.IsActive)
                return Unauthorized(new { message = "Geçersiz veya pasif dükkan kimliği." });

            // [BIZ-01] Trial süresi dolmuş → KDS yazma işlemleri de engellenir
            if (tenant.PlanType == "trial"
                && tenant.TrialEndsAt.HasValue
                && tenant.TrialEndsAt.Value < DateTime.UtcNow)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Deneme süreniz dolmuş. Lütfen aboneliğinizi yenileyin." });
            }

            // ── Order item'ı çek ─────────────────────────────────────────────
            var item = await _db.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o.Table)
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == dto.OrderItemId);

            if (item is null)
                return NotFound(new { message = "Sipariş kalemi bulunamadı." });

            // ── [SEC-03] Adım 3: Çapraz Tenant Doğrulaması ──────────────────
            if (item.Order?.TenantId != tenantId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu sipariş kalemine erişim yetkiniz yok." });

            // ── Durum geçiş doğrulaması ──────────────────────────────────────
            if (!Enum.TryParse<OrderItemStatus>(dto.NewStatus, ignoreCase: true, out var parsedNew))
                return BadRequest(new { message = $"Geçersiz durum değeri: '{dto.NewStatus}'" });

            bool gecerli =
                (item.OrderItemStatus == OrderItemStatus.Pending && parsedNew == OrderItemStatus.Preparing) ||
                (item.OrderItemStatus == OrderItemStatus.Preparing && parsedNew == OrderItemStatus.Ready);

            if (!gecerli)
                return BadRequest(new
                {
                    message = $"Geçersiz geçiş: '{item.OrderItemStatus}' → '{dto.NewStatus}'"
                });

            var tableName = item.Order?.Table?.TableName ?? $"Adisyon #{item.OrderId}";
            var menuItemName = item.MenuItem?.MenuItemName ?? "Ürün";

            // ── [ASYNC-04] State Machine Düzeltmesi ──────────────────────────
            //
            // ESKİ (hatalı):
            //   item.OrderItemStatus = parsedNew == OrderItemStatus.Ready
            //       ? OrderItemStatus.Served     ← Ready gelince Served'e atlıyordu!
            //       : parsedNew;
            //
            // YENİ (doğru):
            //   item.OrderItemStatus = parsedNew;   ← gelen değer ne ise o yazılır
            //
            // Doğru state machine: Pending → Preparing → Ready → (garson servis eder) → Served
            // "Hazır" butonu artık Ready yazar; Served ayrı bir adımdır.
            item.OrderItemStatus = parsedNew;

            await _db.SaveChangesAsync();

            // OrderItemStatusChanged — tüm istemcilere (KDS + garsonlar)
            await _hub.Clients.Group(tenantId).SendAsync("OrderItemStatusChanged", new
            {
                orderItemId = item.OrderItemId,
                newStatus = item.OrderItemStatus.ToString().ToLowerInvariant(), // DB ile tutarlı
                tableName,
                menuItemName
            });

            // OrderReadyForPickup — yalnızca Ready geçişinde garson bildirim alır
            if (parsedNew == OrderItemStatus.Ready)
            {
                await _hub.Clients.Group(tenantId).SendAsync("OrderReadyForPickup", new
                {
                    orderItemId = item.OrderItemId,
                    orderId = item.OrderId,
                    tableName,
                    menuItemName,
                    readyAt = DateTime.Now.ToString("HH:mm:ss")
                });
            }

            return Ok(new { success = true, tableName, menuItemName });
        }
    }

    public class KdsStatusUpdateDto
    {
        public int OrderItemId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}