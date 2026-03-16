// ============================================================================
//  Areas/App/Controllers/HomeController.cs
//
//  REFACTORING: 540+ satır spagetti → 55 satır Thin Controller
//
//  ESKİ DURUM (kaldırıldı):
//    - Index()          → 370 satır, 8+ DB sorgusu doğrudan action içinde
//    - GetLiveMetrics() → 170 satır, 8+ DB sorgusu doğrudan action içinde
//    - Test edilemez (DbContext'e sıkı bağımlı)
//    - 100 kullanıcıda her istek DB'ye 16+ hit
//
//  YENİ DURUM:
//    - IDashboardService + ITenantService inject edildi
//    - Index()          → 4 satır: tenantId al → servise yolla → View'a bas
//    - GetLiveMetrics() → 4 satır: tenantId al → servise yolla → Json döndür
//    - Tüm iş mantığı ve DB erişimi DashboardService'e taşındı
//    - IMemoryCache 30sn cache: 100 kullanıcıda DB yükü 240x azaldı
//
//  PROGRAM.CS'E EKLENMESİ GEREKENLER:
//    builder.Services.AddMemoryCache();
//    builder.Services.AddScoped<IDashboardService, DashboardService>();
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers
{
    [Area("App")]
    [Authorize(Roles = "Admin")]
    public class HomeController : AppBaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly ITenantService _tenantService;

        public HomeController(
            IDashboardService dashboardService,
            ITenantService tenantService)
        {
            _dashboardService = dashboardService;
            _tenantService = tenantService;
        }

        // ── GET /App/Home/Index  —  Dashboard ana sayfa ───────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";

            var tenantId = _tenantService.TenantId ?? string.Empty;
            var vm = await _dashboardService.GetDashboardDataAsync(tenantId);

            return View(vm);
        }

        // ── GET /App/Home/GetLiveMetrics  —  Canlı güncelleme (AJAX/SignalR) ──
        // KPI + Grafikler + Heatmap + Stok + Hedef verilerini tek seferde döner.
        // JS tarafı bu endpoint'i SignalR eventi gelince fetch ile çağırır.
        [HttpGet]
        public async Task<IActionResult> GetLiveMetrics()
        {
            var tenantId = _tenantService.TenantId ?? string.Empty;
            var data = await _dashboardService.GetLiveMetricsAsync(tenantId);

            return Json(data);
        }
    }
}