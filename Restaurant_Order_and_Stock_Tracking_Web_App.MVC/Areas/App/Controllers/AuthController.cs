// ════════════════════════════════════════════════════════════════════════════
//  Areas/App/Controllers/AuthController.cs
//  Yol: Areas/App/Controllers/AuthController.cs
//
//  SPRINT C — [SC-2] Workspace Login Akışı (Güncellendi)
//
//  AŞAMA 1 — Tenant Doğrulama + Timing Attack Koruması
//  AŞAMA 2 — Kullanıcı Adı Birleştirme (TenantId_Username)
//  AŞAMA 3 — Standart Identity Doğrulama (Prefixli arama)
// ════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RestaurantDbContext db)
    {
        _userManager = userManager;
        _db = db;
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
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
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
        // Formdan gelen "varsayilan-restoran" ve "admin" bilgisini birleştiriyoruz
        var fullUsername = $"{model.FirmaKodu.Trim()}_{model.Username.Trim()}";

        // Identity'nin arayabilmesi için bu birleşik ismi büyük harfli (Normalized) formata çeviriyoruz
        var normalizedFullUsername = _userManager.NormalizeName(fullUsername);

        // ════════════════════════════════════════════════════════════════
        //  AŞAMA 3 — Standart Identity Doğrulama
        // ════════════════════════════════════════════════════════════════
        // Adamı kendi yazdığımız Prefixli tam adıyla arıyoruz.
        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.TenantId == model.FirmaKodu &&
            u.NormalizedUserName == normalizedFullUsername);

        if (user == null)
        {
            await Task.Delay(Random.Shared.Next(100, 300));
            ModelState.AddModelError(string.Empty, "Firma kodu, kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        // Lockout kontrolü
        if (await _userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Hesap kilitlendi. 15 dakika sonra tekrar deneyin.");
            return View(model);
        }

        // Şifre kontrolü
        var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordOk)
        {
            await _userManager.AccessFailedAsync(user);
            ModelState.AddModelError(string.Empty, "Firma kodu, kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var roles = await _userManager.GetRolesAsync(user);

        // SysAdmin restoran paneline giremez
        if (roles.Contains("SysAdmin"))
        {
            ModelState.AddModelError(string.Empty, "Bu panele erişim yetkiniz yok.");
            return View(model);
        }

        // ── Başarılı Giriş ────────────────────────────────────────────────
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // AppAuth cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name,           user.UserName!),
            new Claim("FullName",                user.FullName),
            new Claim("TenantId",                user.TenantId ?? ""),
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

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}