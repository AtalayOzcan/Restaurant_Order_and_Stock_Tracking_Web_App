// ============================================================================
//  Services/IDashboardService.cs
//
//  Dashboard servis sözleşmesi.
//
//  NEDEN SERVIS KATMANI?
//  ─────────────────────────────────────────────────────────────────────────
//  HomeController içinde 540+ satır / 8+ DB sorgusu barındıran spagetti yapı
//  bu interface arkasına taşındı:
//    - Controller artık HTTP'nin dilmancısı: al → servise ilet → View'a bas.
//    - DashboardService test edilebilir (DbContext mock'lanabilir).
//    - IMemoryCache tenant-bazlı 30sn önbellekleme: 100 eş zamanlı
//      kullanıcıda her istek DB'ye 8 hit atmak yerine cache'ten okur.
//
//  METOTLAR:
//    GetDashboardDataAsync  → Index() action için DashboardViewModel
//    GetLiveMetricsAsync    → GetLiveMetrics() action için JSON objesi
//                             (SignalR eventi sonrası JS fetch ile çağrılır)
//
//  PARAMETRE: tenantId
//    Cache key izolasyonu için kullanılır.
//    DB sorguları RestaurantDbContext Global Query Filter ile zaten
//    tenant-scope'ludur; parametre yalnızca cache izolasyonu içindir.
// ============================================================================

using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Dashboard;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services
{
    public interface IDashboardService
    {
        /// <summary>
        /// Dashboard ana sayfası için tüm KPI, masa ısı haritası, grafik ve
        /// stok uyarısı verilerini DashboardViewModel olarak döner.
        /// Sonuç tenant bazlı 30 saniye RAM'de önbelleklenir.
        /// </summary>
        /// <param name="tenantId">Cache key izolasyonu için kullanılır.</param>
        Task<DashboardViewModel> GetDashboardDataAsync(string tenantId);

        /// <summary>
        /// Dashboard canlı güncelleme için JSON verisi döner.
        /// SignalR eventi geldiğinde JS bu endpoint'i fetch ile çağırır.
        /// Sonuç tenant bazlı 30 saniye RAM'de önbelleklenir.
        /// </summary>
        /// <param name="tenantId">Cache key izolasyonu için kullanılır.</param>
        Task<object> GetLiveMetricsAsync(string tenantId);
    }
}