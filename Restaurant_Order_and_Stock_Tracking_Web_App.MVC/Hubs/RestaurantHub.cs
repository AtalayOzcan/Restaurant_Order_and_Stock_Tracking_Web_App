// ============================================================================
//  Hubs/RestaurantHub.cs
//  DEĞİŞİKLİK — FAZ 1 ADIM 3: SignalR Tenant İzolasyonu
//
//  EKLENENLER:
//  [SIG-1] OnConnectedAsync  → bağlanan client'ı tenant grubuna ekle
//  [SIG-2] OnDisconnectedAsync → ayrılan client'ı gruptan çıkar
//
//  MANTIK:
//    Her client bağlandığında Claims'teki "TenantId" değeri okunur.
//    Bu değer Groups.AddToGroupAsync için grup adı olarak kullanılır.
//    Artık Clients.Group(tenantId) yalnızca o restoranın bağlı client'larına ulaşır.
//
//    TenantId yoksa (anonim, KDS ekranı vb.) → gruba eklenmez.
//    Controller'lar bu senaryoda Clients.Group(tenantId) çağırırsa
//    o connection zaten grupta olmadığından mesaj ulaşmaz (istenen davranış).
//
//  KİTCHEN (KDS) EKRANI [AllowAnonymous]:
//    KitchenController'da TenantId, ITenantService yerine doğrudan DB'den
//    alınan nesne üzerinden okunur (bkz. KitchenController açıklaması).
//    KDS ekranına websocket üzerinden tenant grubuna dahil olabilmesi için
//    KDS sayfasının JWT/cookie ile kimlik doğrulaması VEYA özel bir
//    "KDS TenantId" query parametresi mekanizması gerekir (ileride eklenecek).
//    Şimdilik KDS → Clients.Group ile doğru broadcast alır;
//    sadece bağlanırken grup ataması yapılmaz.
// ============================================================================
using Microsoft.AspNetCore.SignalR;
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
    /// </summary>
    public class RestaurantHub : Hub
    {
        // ── [SIG-1] Bağlantı Kurulunca — Tenant Grubuna Ekle ────────────────
        // RestaurantHub.cs - OnConnectedAsync metodu
        public override async Task OnConnectedAsync()
        {
            // Kanka, burası en tehlikeli yer. Claim'ler bazen farklı isimlerle gelir. 
            // O yüzden bulabildiğimiz her yerden Dükkan Kodunu arıyoruz.
            var tenantId = Context.User?.FindFirst("TenantId")?.Value
                        ?? Context.User?.FindFirstValue("TenantId")
                        ?? Context.User?.Claims.FirstOrDefault(c => c.Type.Contains("TenantId", StringComparison.OrdinalIgnoreCase))?.Value;

            // Bulduğumuz değeri temizleyelim (boşluk falan varsa silinsin)
            tenantId = tenantId?.Trim();

            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
                Console.WriteLine($"[SIGNALR HUB] {Context.User?.Identity?.Name} isimli garson {tenantId} odasına GİRDİ!");
            }
            else
            {
                Console.WriteLine($"[SIGNALR HUB] DİKKAT! Garsonun dükkan kodu bulunamadı. Lobiye alındı (Odası Yok).");
            }

            await base.OnConnectedAsync();
        }
        
        // ── [SIG-2] Bağlantı Kesilince — Gruptan Çıkar ──────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = Context.User?.FindFirstValue("TenantId");

            if (!string.IsNullOrEmpty(tenantId))
            {
                // SignalR ayrılan connection'ı otomatik temizler,
                // ancak açık çağrı yapılması daha deterministik davranış sağlar.
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Hub metodları intentionally boş:
        // tüm broadcast'ler server-side (controller içinden) IHubContext<RestaurantHub>
        // aracılığıyla yapılır; istemciden hub'a doğrudan çağrı gerekmez.
    }
}