// ============================================================================
//  Areas/App/Controllers/KitchenController.cs  —  KDS v4
//
//  [v4] DEĞİŞİKLİKLER:
//
//  [ISO-1] GQF Tenant İzolasyon Deliği Kapatıldı
//    SORUN  : _tenantService.TenantId anonim (KDS) isteklerde null döner.
//             GQF: "null == null || ..." → koşul her zaman true →
//             tüm tenant'ların siparişleri KDS'e akar!
//    ÇÖZÜM  : IgnoreQueryFilters() + açık WHERE(o => o.TenantId == resolvedId)
//             Böylece GQF'nin null bypass davranışına bağımlı kalmıyoruz.
//
//  [ISO-2] 3-Öncelikli TenantId Çözümleme
//    1. User.FindFirstValue("TenantId")  — kimlikli garson/admin
//    2. Request.Cookies["ros-tenant"]    — anonim KDS tableti (cookie zaten set)
//    3. ?tenantId= query string          — ilk kurulum (henüz cookie yok)
//    Hiçbirinden gelmediyse: boş view döner (veri sızdırmaz).
//
//  [SM-1] State Machine Düzeltildi (Servis Et butonu artık çalışıyor)
//    SORUN  : parsedNew == Ready ? Saved Served : parsedNew
//             → "Hazır" basıldığında item Served olarak kaydediliyor
//             → ServeReadyItems Ready item bulamıyor → "Servis Et" başarısız
//    ÇÖZÜM  : item.OrderItemStatus = parsedNew  (doğrudan ata)
//             Pending  → Preparing  : "Ocağa Al" butonu
//             Preparing → Ready     : "Hazır — Garsonu Çağır" butonu
//             Ready    → Served     : TablesController.ServeReadyItems (garson ekranı)
//
//  [ISO-3] UpdateStatus Cross-Tenant Kontrolü
//    SORUN  : Herhangi bir item'ın ID'si tahmin edilirse başka tenant'ın
//             siparişi güncellenebilirdi.
//    ÇÖZÜM  : item.Order.TenantId == resolvedTenantId kontrolü.
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Shared.Common;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers
{
    [Area("App")]
    [AllowAnonymous]   // KDS tableti giriş gerektirmez; izolasyon cookie ile sağlanır
    public class KitchenController : Controller
    {
        private readonly RestaurantDbContext _db;
        private readonly IHubContext<RestaurantHub> _hub;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<KitchenController> _logger;

        public KitchenController(
            RestaurantDbContext db,
            IHubContext<RestaurantHub> hub,
            IWebHostEnvironment env,
            ILogger<KitchenController> logger)
        {
            _db = db;
            _hub = hub;
            _env = env;
            _logger = logger;
        }

        // ── Yardımcı: TenantId Çözümleme ────────────────────────────────────
        // [ISO-2] Claims → Cookie → QueryString öncelik sırası.
        // KDS AllowAnonymous olduğundan Claims çoğunlukla boş gelir;
        // bu durumda Display() tarafından önceden set edilmiş "ros-tenant"
        // cookie'si kullanılır.
        private string? ResolveTenantId(string? queryStringFallback = null)
        {
            // 1. Kimlikli kullanıcı (garson/admin KDS'e erişirse)
            var fromClaims = User.FindFirstValue("TenantId");
            if (!string.IsNullOrWhiteSpace(fromClaims))
                return fromClaims;

            // 2. Anonim KDS tableti — önceden set edilmiş cookie
            var fromCookie = Request.Cookies["ros-tenant"];
            if (!string.IsNullOrWhiteSpace(fromCookie))
                return fromCookie;

            // 3. İlk kurulum — URL'den tenantId=... parametresi
            if (!string.IsNullOrWhiteSpace(queryStringFallback))
                return queryStringFallback;

            return null;
        }

        // ── GET /App/Kitchen/Display ─────────────────────────────────────────
        public async Task<IActionResult> Display(string? tenantId = null)
        {
            // [ISO-2] TenantId'yi 3 kaynaktan çöz
            var resolvedId = ResolveTenantId(tenantId);

            // TenantId hiçbir yerden gelmediyse: boş sayfa (veri sızdırmaz)
            if (string.IsNullOrEmpty(resolvedId))
            {
                _logger.LogWarning("[KDS] Display() — TenantId çözümlenemedi. Boş ekran döndürülüyor.");
                return View(Enumerable.Empty<Order>());
            }

            // [ISO-1] IgnoreQueryFilters() + açık WHERE — GQF null bypass'ını engeller.
            // GQF: "_tenantService.TenantId == null → tüm tenant'lar" kuralı burada
            // devre dışı bırakılır; sadece bu tenant'ın aktif siparişleri gelir.
            var orders = await _db.Orders
                .IgnoreQueryFilters()
                .Where(o =>
                    o.TenantId == resolvedId &&
                    o.OrderStatus == OrderStatus.Open)
                .Include(o => o.Table)
                .Include(o => o.OrderItems
                    .Where(oi =>
                        oi.OrderItemStatus == OrderItemStatus.Pending ||
                        oi.OrderItemStatus == OrderItemStatus.Preparing))
                    .ThenInclude(oi => oi.MenuItem)
                .OrderBy(o => o.OrderOpenedAt)
                .ToListAsync();

            // Kalem kalmamış (hepsi Served/Cancelled) siparişleri filtrele
            orders = orders.Where(o => o.OrderItems.Any()).ToList();

            // [ISO-1] Cookie'yi çöz ve yaz (yoksa ilk kez; varsa yenile)
            // HttpOnly = false: SignalR JS'nin de okuyabilmesi için
            Response.Cookies.Append("ros-tenant", resolvedId, new CookieOptions
            {
                HttpOnly = false,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            _logger.LogInformation("[KDS] Display() — TenantId: {TenantId}, Aktif Sipariş: {Count}",
                resolvedId, orders.Count);

            return View(orders);
        }

        // ── POST /App/Kitchen/UpdateStatus ──────────────────────────────────
        // State Machine (KDS yönetir):
        //   Pending   → Preparing   ("Ocağa Al" butonu)
        //   Preparing → Ready       ("Hazır — Garsonu Çağır" butonu)
        //
        // Ready → Served geçişi KDS'in sorumluluğu DEĞİLDİR.
        // Bu geçişi TablesController.ServeReadyItems (garson ekranı) yapar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] KdsStatusUpdateDto dto)
        {
            if (dto is null)
                return BadRequest(new { message = "Geçersiz istek gövdesi." });

            // [ISO-2] KDS için TenantId'yi cookie'den çöz
            var resolvedTenantId = ResolveTenantId();
            if (string.IsNullOrEmpty(resolvedTenantId))
            {
                _logger.LogWarning("[KDS] UpdateStatus — TenantId çözümlenemedi. OrderItemId: {Id}", dto.OrderItemId);
                return Unauthorized(new { message = "Tenant kimliği belirlenemedi." });
            }

            // [ISO-1] IgnoreQueryFilters() + Order ve Table include ile çek
            var item = await _db.OrderItems
                .IgnoreQueryFilters()
                .Include(oi => oi.Order).ThenInclude(o => o.Table)
                .Include(oi => oi.MenuItem)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == dto.OrderItemId);

            if (item is null)
                return NotFound(new { message = "Sipariş kalemi bulunamadı." });

            // [ISO-3] Cross-tenant erişim kontrolü
            // KDS sadece kendi tenant'ının item'larını güncelleyebilir.
            if (item.Order?.TenantId != resolvedTenantId)
            {
                _logger.LogWarning(
                    "[KDS] UpdateStatus — Cross-tenant erişim denemesi! " +
                    "İstek TenantId: {ReqTenant}, Item TenantId: {ItemTenant}, OrderItemId: {Id}",
                    resolvedTenantId, item.Order?.TenantId, dto.OrderItemId);
                return StatusCode(403, new { message = "Bu işlem için yetkiniz yok." });
            }

            // String → Enum dönüşümü
            if (!Enum.TryParse<OrderItemStatus>(dto.NewStatus, ignoreCase: true, out var parsedNew))
                return BadRequest(new { message = $"Geçersiz durum değeri: '{dto.NewStatus}'" });

            // [SM-1] State Machine — izin verilen geçişler (KDS'in yetkisi)
            //
            //   Pending   → Preparing ✓  ("Ocağa Al")
            //   Preparing → Ready     ✓  ("Hazır — Garsonu Çağır")
            //
            // DİĞER GEÇİŞLER KDS'İN SORUMLULUĞU DEĞİLDİR:
            //   Ready     → Served    →  TablesController.ServeReadyItems
            //   *         → Cancelled →  OrdersController.CancelItem
            bool gecerli =
                (item.OrderItemStatus == OrderItemStatus.Pending && parsedNew == OrderItemStatus.Preparing) ||
                (item.OrderItemStatus == OrderItemStatus.Preparing && parsedNew == OrderItemStatus.Ready);

            if (!gecerli)
            {
                _logger.LogWarning(
                    "[KDS] UpdateStatus — Geçersiz geçiş: '{Current}' → '{New}', OrderItemId: {Id}",
                    item.OrderItemStatus, dto.NewStatus, dto.OrderItemId);
                return BadRequest(new
                {
                    message = $"Geçersiz geçiş: '{item.OrderItemStatus}' → '{dto.NewStatus}'. " +
                              $"KDS yalnızca Pending→Preparing ve Preparing→Ready geçişlerini yönetir."
                });
            }

            var tableName = item.Order?.Table?.TableName ?? $"Adisyon #{item.OrderId}";
            var menuItemName = item.MenuItem?.MenuItemName ?? "Ürün";

            // [SM-1] Durumu DOĞRUDAN kaydet — eski kod Ready'yi Served'e çeviriyordu.
            // Bu hata "Servis Et" butonunun çalışmamasına yol açıyordu.
            item.OrderItemStatus = parsedNew;
            await _db.SaveChangesAsync();

            // ── SignalR: OrderItemStatusChanged ─────────────────────────────
            // KDS ve adisyon detay sayfası bu event'i dinler.
            try
            {
                await _hub.Clients.Group(resolvedTenantId).SendAsync("OrderItemStatusChanged", new
                {
                    orderItemId = item.OrderItemId,
                    newStatus = dto.NewStatus,   // camelCase — JS ile eşleşir
                    tableName,
                    menuItemName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[KDS] OrderItemStatusChanged gönderilemedi — TenantGroup: {Group}, ItemId: {Id}",
                    resolvedTenantId, item.OrderItemId);
            }

            // ── SignalR: OrderReadyForPickup (sadece Ready geçişinde) ────────
            // Tables ekranı bu event'i dinler:
            //   • "Sipariş Hazır!" rozeti ekler
            //   • "Servis Et" butonu gösterir
            if (parsedNew == OrderItemStatus.Ready)
            {
                try
                {
                    await _hub.Clients.Group(resolvedTenantId).SendAsync("OrderReadyForPickup", new
                    {
                        orderItemId = item.OrderItemId,
                        orderId = item.OrderId,
                        tableName,
                        menuItemName,
                        readyAt = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[KDS] OrderReadyForPickup gönderilemedi — TenantGroup: {Group}, ItemId: {Id}",
                        resolvedTenantId, item.OrderItemId);
                }
            }

            _logger.LogInformation(
                "[KDS] UpdateStatus OK — TenantId: {Tenant}, Table: {Table}, Item: {Item}, {Old}→{New}",
                resolvedTenantId, tableName, menuItemName, item.OrderItemStatus, dto.NewStatus);

            return Ok(new { success = true, tableName, menuItemName });
        }
    }

    // ── DTO ──────────────────────────────────────────────────────────────────
    public class KdsStatusUpdateDto
    {
        public int OrderItemId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}