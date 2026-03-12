// ============================================================================
//  Areas/App/Controllers/SubscriptionController.cs
//
//  SPRINT C — Paywall Ekranı
//  [SC-8] Yeni controller: deneme süresi biten kullanıcıları karşılar.
//         AppBaseController'dan türer → [Area("App")] + [Authorize(AppAuth)] miras alınır.
//         OnActionExecutionAsync paywall filtresi bu controller için bypass edilir.
// ============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers
{
    public class SubscriptionController : AppBaseController
    {
        private readonly RestaurantDbContext _db;

        public SubscriptionController(RestaurantDbContext db)
        {
            _db = db;
        }

        // ── GET /App/Subscription/Index ───────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var tenantId = User.FindFirstValue("TenantId");

            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

            ViewData["Title"] = "Abonelik Planları";
            ViewBag.RestaurantName = tenant?.Name ?? "";
            ViewBag.TrialEndsAt = tenant?.TrialEndsAt;
            ViewBag.PlanType = tenant?.PlanType ?? "trial";

            return View();
        }
    }
}