// ============================================================================
//  Areas/App/Controllers/AppBaseController.cs
//
//  SPRINT C DEĞİŞİKLİKLERİ:
//  [SC-7] OnActionExecutionAsync → Paywall (Trial bitiş) filtresi eklendi
//         - Tenant.TrialEndsAt < DateTime.UtcNow → /App/Subscription/Index yönlendir
//         - Sonsuz döngü koruması: SubscriptionController ve AuthController muaf
//         - PlanType == "trial" olmayan (ücretli) tenant'lar bypass edilir
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers
{
    [Area("App")]
    [Authorize(AuthenticationSchemes = "AppAuth")]
    public abstract class AppBaseController : Controller
    {
        // [SC-7] Paywall filtresi
        // OnActionExecutionAsync: her action çalışmadan önce tetiklenir.
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // ── Sonsuz döngü koruması ────────────────────────────────────────
            // SubscriptionController'a gidilirken kontrol yapma (zaten oradayız).
            // AuthController'a gidilirken yapma (kullanıcı çıkış yapabilmeli).
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            if (controllerName is "Subscription" or "Auth")
            {
                await next();
                return;
            }

            // ── TenantId al ──────────────────────────────────────────────────
            var tenantId = context.HttpContext.User.FindFirstValue("TenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                await next();
                return;
            }

            // ── DB'den tenant'ı çek ve trial kontrolü yap ───────────────────
            // IServiceProvider üzerinden DbContext alıyoruz; base constructor
            // değiştirmeden DI çözümlemenin en temiz yolu budur.
            var db = context.HttpContext.RequestServices
                .GetRequiredService<RestaurantDbContext>();

            var tenant = await db.Tenants
                .AsNoTracking()
                .Select(t => new { t.TenantId, t.PlanType, t.TrialEndsAt, t.IsActive })
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

            // Tenant bulunamazsa veya pasifse normal auth akışına bırak
            if (tenant is null || !tenant.IsActive)
            {
                await next();
                return;
            }

            // Ücretli plan → paywall muaf
            if (tenant.PlanType != "trial")
            {
                await next();
                return;
            }

            // Trial plan + süre dolmuşsa → Subscription sayfasına yönlendir
            if (tenant.TrialEndsAt.HasValue && tenant.TrialEndsAt.Value < DateTime.UtcNow)
            {
                context.Result = RedirectToAction(
                    actionName: "Index",
                    controllerName: "Subscription",
                    routeValues: new { area = "App" });
                return;
            }

            // Kontroller geçti → action'ı çalıştır
            await next();
        }
    }
}