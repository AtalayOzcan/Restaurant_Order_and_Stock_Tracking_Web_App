// ============================================================================
//  Middlewares/GlobalExceptionMiddleware.cs
//
//  SPRINT 3 — [SEC-EX] Global Exception Handler
//
//  NEDEN BU MIDDLEWARE?
//  ─────────────────────────────────────────────────────────────────────────
//  ESKİ DURUM (hatalı):
//    app.UseExceptionHandler("/Landing/Index")  → sadece view yönlendirmesi.
//    AJAX/fetch isteğinde exception olunca HTML 500 sayfası döner.
//    JS bu HTML'i parse edemez → UI'da belirsiz hata veya crash.
//    Hiçbir exception ILogger ile loglanmıyor → debug imkânsız.
//
//  YENİ DURUM:
//    İstek türüne göre akıllı yanıt:
//      AJAX / fetch  → { "success": false, "message": "..." } (JSON)
//      Normal View   → /Error sayfasına redirect
//    Her exception ILogger.LogError ile structured log olarak kaydedilir.
//    Development modunda kullanıcıya teknik detay gösterilebilir.
//    Production'da stack trace/dosya yolu asla ifşa edilmez.
//
//  KULLANIM (Program.cs):
//    app.UseMiddleware<GlobalExceptionMiddleware>();  // pipeline'ın en başına
//    app.UseStatusCodePagesWithReExecute("/Error/{0}");
// ============================================================================

using Microsoft.AspNetCore.Diagnostics;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                // ── Yapısal Loglama ───────────────────────────────────────────
                // Tüm beklenmeyen exception'lar burada loglanır.
                // Structured log: Method, Path ve Message ayrı alan olarak
                // kaydedilir → Seq/Serilog gibi aggregator'larda filtrelenebilir.
                _logger.LogError(ex,
                    "[GlobalException] {Method} {Path} — {ExceptionType}: {Message}",
                    ctx.Request.Method,
                    ctx.Request.Path,
                    ex.GetType().Name,
                    ex.Message);

                // Yanıt zaten başladıysa (stream'e yazılmışsa) müdahale edemeyiz.
                // Bağlantıyı kapatmaktan başka seçenek yoktur.
                if (ctx.Response.HasStarted)
                {
                    _logger.LogWarning(
                        "[GlobalException] Yanıt zaten başlamış, hata sayfası gösterilemiyor. Path: {Path}",
                        ctx.Request.Path);
                    throw; // orijinal exception'ı yeniden fırlat
                }

                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.Headers.Clear();

                // ── İstek Türü Tespiti ────────────────────────────────────────
                // AJAX / fetch istekleri JSON yanıt bekler.
                // X-Requested-With veya Accept: application/json başlıklarına bakılır.
                bool isAjaxOrApi =
                    ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    ctx.Request.Headers["Accept"]
                        .ToString()
                        .Contains("application/json", StringComparison.OrdinalIgnoreCase);

                // Development'ta kullanıcıya (geliştirici) teknik mesaj göster.
                // Production'da nötr mesaj döner — stack trace/dosya yolu ifşa edilmez.
                string userMessage = _env.IsDevelopment()
                    ? $"[DEV] {ex.GetType().Name}: {ex.Message}"
                    : "Sistemde anlık bir sorun oluştu. Lütfen tekrar deneyin.";

                if (isAjaxOrApi)
                {
                    // ── AJAX / Fetch → JSON yanıt ─────────────────────────────
                    ctx.Response.ContentType = "application/json; charset=utf-8";

                    // JSON manuel oluşturuluyor — System.Text.Json kullanımı
                    // DI gerektirmez ve bu katmanda servis inject etmek risklidir.
                    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = userMessage
                    });

                    await ctx.Response.WriteAsync(jsonResponse);
                }
                else
                {
                    // ── Normal View isteği → /Error sayfasına yönlendir ────────
                    // Redirect yerine ReExecute kullanıyoruz (URL aynı kalır,
                    // kullanıcı arka planda /Error sayfasını görür).
                    ctx.Response.Redirect("/Error");
                }
            }
        }
    }
}