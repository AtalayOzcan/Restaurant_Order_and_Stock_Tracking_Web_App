// ════════════════════════════════════════════════════════════════════════════
//  Areas/App/Controllers/AuthController.cs
//  Yol: Areas/App/Controllers/AuthController.cs
//
//  SPRINT C — [SC-2] Workspace Login Akışı (Güncellendi)
//
//  AŞAMA 1 — Tenant Doğrulama + Timing Attack Koruması
//  AŞAMA 2 — Kullanıcı Adı Birleştirme (TenantId_Username)
//  AŞAMA 3 — Standart Identity Doğrulama (Prefixli arama)
//
//  SPRINT 4 — [IMP-4] Impersonation Giriş ve Çıkış Action'ları
//  Impersonate    : Token doğrulayıp AppAuth cookie set eder (atomik UPDATE)
//  EndImpersonation: AppAuth cookie siler, /Admin paneline yönlendirir
// ════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Auth;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers;

[Area("App")]
public class AuthController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RestaurantDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RestaurantDbContext db,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    // ── GET /App/Auth/Login ────────────────────────────────────────────────
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Tables", new { area = "App" });

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ── POST /App/Auth/Login ───────────────────────────────────────────────
    // [SEC-RL-1] LoginPolicy: 60 saniyede 10 istek — brute-force koruması.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // ════════════════════════════════════════════════════════════════
        //  AŞAMA 1 — Tenant Doğrulama + Timing Attack Koruması
        // ════════════════════════════════════════════════════════════════
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == model.FirmaKodu);

        bool tenantInvalid =
            tenant == null ||
            !tenant.IsActive ||
            (tenant.TrialEndsAt.HasValue && tenant.TrialEndsAt.Value < DateTime.UtcNow);

        if (tenantInvalid)
        {
            await Task.Delay(Random.Shared.Next(100, 300));
            ModelState.AddModelError(string.Empty, "Firma kodu, kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        // ════════════════════════════════════════════════════════════════
        //  AŞAMA 2 — Kullanıcı Adı Birleştirme (Prefix Oluşturma)
        // ════════════════════════════════════════════════════════════════
        var fullUsername = $"{model.FirmaKodu.Trim()}_{model.Username.Trim()}";
        var normalizedFullUsername = _userManager.NormalizeName(fullUsername);

        // ════════════════════════════════════════════════════════════════
        //  AŞAMA 3 — Standart Identity Doğrulama
        // ════════════════════════════════════════════════════════════════
        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.TenantId == model.FirmaKodu &&
            u.NormalizedUserName == normalizedFullUsername);

        if (user == null)
        {
            await Task.Delay(Random.Shared.Next(100, 300));
            ModelState.AddModelError(string.Empty, "Firma kodu, kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Hesap kilitlendi. 15 dakika sonra tekrar deneyin.");
            return View(model);
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordOk)
        {
            await _userManager.AccessFailedAsync(user);
            ModelState.AddModelError(string.Empty, "Firma kodu, kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("SysAdmin"))
        {
            ModelState.AddModelError(string.Empty, "Bu panele erişim yetkiniz yok.");
            return View(model);
        }

        // ── Başarılı Giriş ─────────────────────────────────────────────
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name,           user.UserName!),
            new("FullName",                user.FullName),
            new("TenantId",                user.TenantId ?? ""),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "AppAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AppAuth", principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (roles.Contains("Kitchen"))
            return RedirectToAction("Display", "Kitchen", new { area = "App" });

        return RedirectToAction("Index", "Tables", new { area = "App" });
    }

    // ── POST /App/Auth/Logout ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AppAuth");
        return RedirectToAction(nameof(Login), new { area = "App" });
    }

    // ── GET /App/Auth/Impersonate?token={uuid} ────────────────────────────
    // [IMP-4] Token doğrula → AppAuth cookie set → Admin rolüyle tenant'a gir.
    //
    // GÜVENLİK:
    //   Atomik UPDATE: token bul + kullanıldı işaretle tek sorguda yapılır.
    //   Race condition imkânsız — iki eş zamanlı istek gelirse biri 0 satır
    //   döner ve 403 alır.
    //
    //   AdminAuth cookie'ye hiç dokunulmaz. SysAdmin Sekme 1'deki admin
    //   panelini kaybetmez.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Impersonate(string token)
    {
        if (!Guid.TryParse(token, out var tokenGuid))
        {
            _logger.LogWarning("[IMPERSONATION] Geçersiz token formatı. IP: {Ip}", GetClientIp());
            return Forbid();
        }

        // ── Atomik token tüketimi ─────────────────────────────────────────
        // UPDATE ... WHERE used_at IS NULL AND expires_at > NOW() RETURNING *
        // Onaylanan karar: race condition koruması için tek sorgu.
        //
        // EF Core FromSqlRaw ile atomik PostgreSQL sorgusu:
        var clientIp = GetClientIp();
        var now = DateTime.UtcNow;

        // EF Core tracked update — optimistic locking ile eşdeğer:
        // 1. Token'ı bul (WHERE used_at IS NULL AND expires_at > NOW())
        // 2. Aynı işlemde used_at set et
        // 3. SaveChanges xmin token sayesinde başka biri aynı anda
        //    değiştirdiyse DbUpdateConcurrencyException fırlar → 403
        var record = await _db.ImpersonationTokens
            .FirstOrDefaultAsync(t =>
                t.TokenId == tokenGuid &&
                t.UsedAt == null &&
                t.ExpiresAt > now);

        if (record == null)
        {
            _logger.LogWarning(
                "[IMPERSONATION] Token geçersiz, süresi dolmuş veya daha önce kullanılmış. TokenId: {TokenId} | IP: {Ip}",
                tokenGuid, clientIp);
            return Forbid();
        }

        // Token'ı anında işaretle — kullanıldı
        record.UsedAt = now;
        record.UsedFromIp = clientIp;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Başka bir istek milisaniye farkla aynı token'ı tüketti
            _logger.LogWarning(
                "[IMPERSONATION] Race condition tespit edildi! TokenId: {TokenId} | IP: {Ip}",
                tokenGuid, clientIp);
            return Forbid();
        }

        // ── Hedef tenant'ın Admin kullanıcısını bul ───────────────────────
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var targetAdmin = adminUsers.FirstOrDefault(u => u.TenantId == record.TargetTenantId);

        if (targetAdmin == null)
        {
            _logger.LogError(
                "[IMPERSONATION] Hedef tenanta ait Admin kullanıcı bulunamadı. TenantId: {TenantId}",
                record.TargetTenantId);
            return NotFound("Bu restorana ait yönetici hesabı bulunamadı.");
        }

        // ── AppAuth cookie set et — IsImpersonation claim ile ─────────────
        // AdminAuth cookie'ye HİÇ DOKUNMA. Bu claim'ler yeni bir AppAuth
        // oturumu oluşturur; mevcut AdminAuth oturumu canlı kalmaya devam eder.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, targetAdmin.Id),
            new(ClaimTypes.Name,           targetAdmin.UserName ?? ""),
            new("FullName",                targetAdmin.FullName ?? ""),
            new("TenantId",                record.TargetTenantId),
            new(ClaimTypes.Role,           record.TargetRole),
            // ── Impersonation işaretleri ──
            new("IsImpersonation", "true"),                     // Banner için
            new("ImpersonatedBy",  record.SysAdminId),          // Audit log için
        };

        var identity = new ClaimsIdentity(claims, "AppAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AppAuth", principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)  // Onaylanan karar: 30 dk
        });

        _logger.LogWarning(
            "[IMPERSONATION] Giriş başarılı. SysAdmin: {SysAdminId} → Tenant: {TenantId} | IP: {Ip} | TokenId: {TokenId}",
            record.SysAdminId, record.TargetTenantId, clientIp, tokenGuid);

        return RedirectToAction("Index", "Tables", new { area = "App" });
    }

    // ── POST /App/Auth/EndImpersonation ───────────────────────────────────
    // [IMP-4] "Admin paneline dön" butonu — AppAuth sil, Admin paneline git.
    //
    // AdminAuth cookie'ye HİÇ DOKUNMAZ.
    // SysAdmin Sekme 1'deki admin panelini kaybetmez.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndImpersonation()
    {
        // Sadece impersonation oturumunda çalışsın
        var isImpersonation = User.FindFirstValue("IsImpersonation") == "true";
        var sysAdminId = User.FindFirstValue("ImpersonatedBy") ?? "?";
        var tenantId = User.FindFirstValue("TenantId") ?? "?";

        if (!isImpersonation)
            return RedirectToAction("Index", "Tables", new { area = "App" });

        // AppAuth cookie'yi sil
        await HttpContext.SignOutAsync("AppAuth");

        _logger.LogWarning(
            "[IMPERSONATION] Oturum sonlandırıldı. SysAdmin: {SysAdminId} ← Tenant: {TenantId} | IP: {Ip}",
            sysAdminId, tenantId, GetClientIp());

        // Admin paneline yönlendir — AdminAuth hâlâ canlı
        return Redirect("/Admin/Home/Index");
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private string GetClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
        ?? "bilinmiyor";
}