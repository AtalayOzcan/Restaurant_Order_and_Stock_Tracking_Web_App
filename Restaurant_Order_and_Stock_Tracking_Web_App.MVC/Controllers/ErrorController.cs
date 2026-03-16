// ============================================================================
//  Controllers/ErrorController.cs
//
//  SPRINT 3 — [SEC-EX] Hata Sayfası Controller'ı
//
//  NEDEN BU CONTROLLER?
//  ─────────────────────────────────────────────────────────────────────────
//  UseStatusCodePagesWithReExecute("/Error/{0}") ve
//  GlobalExceptionMiddleware → /Error yönlendirmesi bu controller'a gelir.
//
//  ROTALAR:
//    GET /Error          → 500 genel hata sayfası
//    GET /Error/404      → Bulunamadı sayfası
//    GET /Error/403      → Yetkisiz erişim sayfası
//    GET /Error/{diğer}  → Genel hata sayfası
//
//  GÜVENLİK:
//    [AllowAnonymous] → Oturumu olmayan kullanıcı da hata sayfasını görebilir.
//    Production'da exception detayı hiçbir zaman View'a geçirilmez.
//    Development'ta path bilgisi ViewBag'e eklenir (debug kolaylığı).
//
//  VİEW BEKLENEN KONUMLAR:
//    Views/Error/Index.cshtml      → Genel 500 hatası
//    Views/Error/NotFound.cshtml   → 404 Bulunamadı
//    Views/Error/Forbidden.cshtml  → 403 Yetkisiz
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    /// <summary>
    /// Tüm HTTP hata kodları ve beklenmeyen exception'lar için merkezi hata controller'ı.
    /// [AllowAnonymous]: Oturumu olmayan kullanıcılar da hata sayfasını görebilmeli.
    /// </summary>
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorController(
            ILogger<ErrorController> logger,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // ── GET /Error ────────────────────────────────────────────────────────
        // GlobalExceptionMiddleware ve genel 500 hataları buraya gelir.
        [Route("/Error")]
        public IActionResult Index()
        {
            // IExceptionHandlerPathFeature: exception'ın hangi endpoint'te
            // fırladığını ve exception nesnesini taşır.
            var exceptionFeature = HttpContext.Features
                .Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature?.Error != null)
            {
                _logger.LogWarning(
                    "[ErrorController] 500 hata sayfası gösteriliyor. " +
                    "Hatalı path: {Path}, Exception türü: {ExceptionType}",
                    exceptionFeature.Path,
                    exceptionFeature.Error.GetType().Name);
            }

            // Development modunda path bilgisi View'a geçirilir (debug kolaylığı).
            // Production'da ViewBag boş kalır — teknik bilgi ifşa edilmez.
            if (_env.IsDevelopment() && exceptionFeature != null)
            {
                ViewBag.FailedPath = exceptionFeature.Path;
                ViewBag.ExceptionType = exceptionFeature.Error?.GetType().Name;
                ViewBag.ExceptionMsg = exceptionFeature.Error?.Message;
            }

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View("Index"); // Views/Error/Index.cshtml
        }

        // ── GET /Error/{statusCode} ───────────────────────────────────────────
        // UseStatusCodePagesWithReExecute("/Error/{0}") bu rotayı çağırır.
        // 404, 403 ve diğer HTTP hata kodları buraya gelir.
        [Route("/Error/{statusCode:int}")]
        public IActionResult StatusCode(int statusCode)
        {
            var statusFeature = HttpContext.Features
                .Get<IStatusCodeReExecuteFeature>();

            _logger.LogWarning(
                "[ErrorController] HTTP {StatusCode} hatası. Orijinal path: {OriginalPath}",
                statusCode,
                statusFeature?.OriginalPath ?? "bilinmiyor");

            Response.StatusCode = statusCode;

            return statusCode switch
            {
                // 404 — Sayfa / Kaynak Bulunamadı
                StatusCodes.Status404NotFound
                    => View("NotFound"),    // Views/Error/NotFound.cshtml

                // 403 — Yetkisiz Erişim
                StatusCodes.Status403Forbidden
                    => View("Forbidden"),   // Views/Error/Forbidden.cshtml

                // 401 — Oturum Açılmamış
                StatusCodes.Status401Unauthorized
                    => View("Forbidden"),   // Aynı "yetkisiz" sayfası yeterli

                // Diğer tüm durum kodları → genel hata sayfası
                _ => View("Index")          // Views/Error/Index.cshtml
            };
        }
    }
}