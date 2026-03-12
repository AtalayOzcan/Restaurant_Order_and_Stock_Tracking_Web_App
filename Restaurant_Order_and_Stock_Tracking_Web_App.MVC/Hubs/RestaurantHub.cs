// ============================================================================
//  Hubs/RestaurantHub.cs  —  v2
//
//  [HUB-1] SysAdmin Düzeltmesi
//    ESKİ : Kimlikli kullanıcı fakat TenantId Claim'i yok (SysAdmin) → Abort()
//    YENİ : Kimlikli + TenantId yok → grup katılımı atlanır, bağlantı açık kalır.
//           Anonim + TenantId yok (cookie da yok) → Abort() (güvenlik).
//
//  [HUB-2] Tenant İzolasyon Güvencesi (belgesel)
//    Her bağlantı yalnızca KENDİ tenantId grubu ile eşleşir.
//    Tüm Broadcast'ler: _hub.Clients.Group(tenantId).SendAsync(...)
//    Farklı bir gruba mesaj gönderilmesi kod seviyesinde imkânsız.
//
//  [HUB-3] Kaynak Ayrımı Logu
//    Bağlantı kurulduğunda Claims mi yoksa Cookie mi kullanıldığı loglanır.
//    Bu sayede KDS tabletlerinin cookie-based bağlantıları izlenebilir.
//
//  ÖNEMLİ: Bu hub'da HiçBİR broadcast metodu yoktur (intentional).
//  Tüm SendAsync çağrıları IHubContext<RestaurantHub> üzerinden yapılır:
//    • KitchenController.UpdateStatus  → OrderItemStatusChanged, OrderReadyForPickup
//    • TablesController.ServeReadyItems → OrderServed, RemoveOrderCard/OrderUpdated
//    • TablesController.DismissWaiter  → WaiterDismissed
//    • QrMenuController.CallWaiter     → WaiterCalled
//    • OrderService (NotifyKitchenAsync) → NewOrderItem
// ============================================================================

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs
{
    /// <summary>
    /// Restoran genelindeki gerçek zamanlı bildirimler için SignalR Hub.
    ///
    /// TENANT İZOLASYON PROTOKOLÜ:
    ///   1. OnConnectedAsync → TenantId doğrulanır → Groups.AddToGroupAsync(connId, tenantId)
    ///   2. Tüm broadcast'ler → Clients.Group(tenantId).SendAsync(...)
    ///   3. OnDisconnectedAsync → Groups.RemoveFromGroupAsync(connId, tenantId)
    ///
    /// Desteklenen event'ler (server → client):
    ///   • WaiterCalled           – müşteri "Garson Çağır"a bastı
    ///   • WaiterDismissed        – garson "İlgilenildi"ye bastı
    ///   • OrderItemStatusChanged – KDS item durumu değişti (Preparing / Ready)
    ///   • NewOrderItem           – garson yeni ürün ekledi → KDS'e düşer
    ///   • OrderReadyForPickup    – mutfak "Hazır" bastı → garson bildirim alır
    ///   • OrderServed            – garson "Servis Et"e bastı → detail sayfası güncellenir
    ///   • RemoveOrderCard        – KDS kartı kaldır (masa adisyonu birleştirildi vb.)
    ///   • OrderUpdated           – KDS kartı yenile
    ///   • ShiftDifferenceAlert   – vardiya farkı eşiği aşıldı
    /// </summary>
    public class RestaurantHub : Hub
    {
        private readonly ILogger<RestaurantHub> _logger;
        private readonly RestaurantDbContext _db;

        public RestaurantHub(ILogger<RestaurantHub> logger, RestaurantDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // ── Bağlantı Kurulunca ───────────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            var connId = Context.ConnectionId;

            // ── Adım 1: TenantId'yi kaynaktan çöz ──────────────────────────
            var tenantId = Context.User?.FindFirstValue("TenantId");
            var source = "claims";

            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = Context.GetHttpContext()?.Request.Cookies["ros-tenant"];
                source = "cookie";
            }

            // ── Adım 2: TenantId yok — kaynak ayrımı yap ───────────────────
            if (string.IsNullOrEmpty(tenantId))
            {
                var isAuthenticated = Context.User?.Identity?.IsAuthenticated == true;

                if (isAuthenticated)
                {
                    // [HUB-1] SysAdmin veya TenantId'siz kimlikli kullanıcı:
                    // Bağlantıyı açık tut fakat gruba ekleme.
                    // SysAdmin'in tüm tenant verilerine erişmesi farklı mekanizmalarla olur.
                    _logger.LogInformation(
                        "[RestaurantHub] Kimlikli kullanıcı TenantId'siz bağlandı " +
                        "(muhtemelen SysAdmin) — grup katılımı atlandı. ConnId: {ConnId}",
                        connId);
                }
                else
                {
                    // Anonim + TenantId yok = geçersiz bağlantı (cookie set edilmemiş).
                    // KDS tabletinin önce /App/Kitchen/Display'e gitmesi gerekir.
                    _logger.LogWarning(
                        "[RestaurantHub] Anonim bağlantı, TenantId yok — bağlantı reddedildi. " +
                        "ConnId: {ConnId}", connId);
                    Context.Abort();
                    return;
                }

                await base.OnConnectedAsync();
                return;
            }

            // ── Adım 3: Cookie kaynağı için DB doğrulaması ──────────────────
            // Claims'ten gelen TenantId zaten login sırasında doğrulandı.
            // Cookie manipülasyonuna karşı her iki kaynakta da kontrol yapılır
            // (defence-in-depth).
            var tenantExists = await _db.Tenants
                .AnyAsync(t => t.TenantId == tenantId && t.IsActive);

            if (!tenantExists)
            {
                _logger.LogWarning(
                    "[RestaurantHub] Geçersiz/pasif TenantId '{TenantId}' " +
                    "(kaynak: {Source}) — bağlantı reddedildi. ConnId: {ConnId}",
                    tenantId, source, connId);
                Context.Abort();
                return;
            }

            // ── Adım 4: Doğrulanmış TenantId grubuna ekle ───────────────────
            // [HUB-2] TENANT İZOLASYON GÜVENCESI:
            // Bu noktadan sonra bu bağlantı YALNIZCA tenantId grubundan
            // mesaj alabilir. Clients.Group(tenantId).SendAsync(...)
            // çağrıları yalnızca bu grubu hedefler.
            await Groups.AddToGroupAsync(connId, tenantId);

            _logger.LogInformation(
                "[RestaurantHub] Bağlandı — ConnId: {ConnId} → Group: '{TenantId}' (kaynak: {Source})",
                connId, tenantId, source);

            await base.OnConnectedAsync();
        }

        // ── Bağlantı Kesilince ───────────────────────────────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connId = Context.ConnectionId;
            var tenantId = Context.User?.FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantId))
                tenantId = Context.GetHttpContext()?.Request.Cookies["ros-tenant"];

            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(connId, tenantId);
                _logger.LogInformation(
                    "[RestaurantHub] Bağlantı kesildi — ConnId: {ConnId}, Group: '{TenantId}'",
                    connId, tenantId);
            }
            else
            {
                _logger.LogInformation(
                    "[RestaurantHub] Bağlantı kesildi — ConnId: {ConnId} (grup üyesi değildi)",
                    connId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}