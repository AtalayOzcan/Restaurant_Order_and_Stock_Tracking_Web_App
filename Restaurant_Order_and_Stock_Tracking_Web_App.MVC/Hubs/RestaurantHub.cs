// ============================================================================
//  Hubs/RestaurantHub.cs
//  SPRINT B.2 — KDS Cookie Güvenlik Açığı Kapatması
//
//  DEĞİŞİKLİKLER:
//  [SEC-1] RestaurantDbContext inject edildi
//  [SEC-2] OnConnectedAsync → Cookie'den okunan TenantId DB'de doğrulanıyor
//  [SEC-3] Doğrulanamayan bağlantılar gruba eklenmiyor + Context.Abort() ile reddediliyor
//
//  GEREKÇE:
//    Önceki implementasyonda cookie değeri kör güvenle kabul ediliyordu.
//    Kötü niyetli kullanıcı "ros-tenant" cookie'sini manipüle ederek
//    başka bir tenant'ın KDS grubuna girebiliyordu.
// ============================================================================
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs
{
    /// <summary>
    /// Restoran genelindeki gerçek zamanlı bildirimler için SignalR Hub.
    /// Desteklenen event'ler (server → client):
    ///   • WaiterCalled           – müşteri "Garson Çağır"a bastı
    ///   • WaiterDismissed        – garson "İlgilenildi"ye bastı
    ///   • OrderItemStatusChanged – KDS item durumu değişti
    ///   • NewOrderItem           – garson yeni ürün ekledi → KDS'e düşer
    ///   • OrderReadyForPickup    – mutfak "Hazır" bastı → garson bildirim alır
    ///   • OrderServed            – garson servis etti → detail sayfası yenilenir
    ///   • RemoveOrderCard        – KDS kartı kaldır
    ///   • OrderUpdated           – KDS kartı güncelle
    ///   • ShiftDifferenceAlert   – vardiya farkı eşiği aşıldı
    /// </summary>
    public class RestaurantHub : Hub
    {
        private readonly ILogger<RestaurantHub> _logger;

        // [SEC-1] Tenant doğrulaması için DbContext inject edildi.
        // IDbContextFactory yerine doğrudan DbContext kullanılıyor:
        // Hub scope'u zaten per-connection olduğundan lifetime uyumlu.
        private readonly RestaurantDbContext _db;

        public RestaurantHub(ILogger<RestaurantHub> logger, RestaurantDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // ── [SEC-2] Bağlantı Kurulunca — Tenant Doğrula + Gruba Ekle ────────
        public override async Task OnConnectedAsync()
        {
            // 1. Önce Claims (kimlikli kullanıcılar: garson, kasiyer, admin)
            var tenantId = Context.User?.FindFirstValue("TenantId");
            var source = "claims";

            // 2. Claims boşsa → Cookie fallback (KDS — AllowAnonymous)
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = Context.GetHttpContext()?.Request.Cookies["ros-tenant"];
                source = "cookie";
            }

            // 3. Her iki kaynaktan da TenantId gelmemişse → reddet
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning(
                    "[RestaurantHub] TenantId yok — ConnectionId: {ConnectionId} reddedildi.",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            // [SEC-2] Cookie'den gelen değeri kör kabul etme:
            // DB'de gerçekten böyle aktif bir tenant var mı diye doğrula.
            // Claims'ten gelen değer zaten login sırasında doğrulandığından
            // sadece cookie kaynağında kontrol yeterlidir; ancak her iki
            // kaynağı da kontrol etmek defence-in-depth sağlar.
            var tenantExists = await _db.Tenants
                .AnyAsync(t => t.TenantId == tenantId && t.IsActive);

            if (!tenantExists)
            {
                // [SEC-3] Geçersiz / manipüle edilmiş / pasif tenant → bağlantıyı kes
                _logger.LogWarning(
                    "[RestaurantHub] Geçersiz veya pasif TenantId '{TenantId}' — " +
                    "kaynak: {Source}, ConnectionId: {ConnectionId}. Bağlantı reddedildi.",
                    tenantId, source, Context.ConnectionId);
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);

            _logger.LogInformation(
                "[RestaurantHub] Bağlandı — ConnectionId: {ConnectionId} → " +
                "Group: {TenantId} (kaynak: {Source})",
                Context.ConnectionId, tenantId, source);

            await base.OnConnectedAsync();
        }

        // ── Bağlantı Kesilince — Gruptan Çıkar ──────────────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = Context.User?.FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantId))
                tenantId = Context.GetHttpContext()?.Request.Cookies["ros-tenant"];

            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
                _logger.LogInformation(
                    "[RestaurantHub] Bağlantı kesildi — ConnectionId: {ConnectionId}, Group: {TenantId}",
                    Context.ConnectionId, tenantId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Hub metodları intentionally boş.
        // Tüm broadcast'ler IHubContext<RestaurantHub> üzerinden yapılır.
    }
}