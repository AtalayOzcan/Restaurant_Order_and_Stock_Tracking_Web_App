using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Auth;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // ── GET /Auth/Login ──────────────────────────────────────────────
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Index", "Tables");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ── POST /Auth/Login ─────────────────────────────────────────────
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // Tekil oturum: sonraki request'lerde eski cookie'ler geçersiz kalır
            await _userManager.UpdateSecurityStampAsync(user);

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // PasswordSignInAsync sonrası User.IsInRole() çalışmaz (cookie
            // henüz bu request'e işlenmedi) → DB'den oku
            var roles = await _userManager.GetRolesAsync(user);

            // Rolsüz kullanıcı — Admin panelinden düzeltilmeli
            if (roles.Count == 0)
            {
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty,
                    "Hesabınıza henüz bir rol atanmamış. Lütfen Admin ile iletişime geçin.");
                return View(model);
            }

            // Admin → Dashboard, diğer tüm roller (Garson, Kasiyer) → Masalar
            if (roles.Contains("Admin"))
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Tables");
        }

        ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
        return View(model);
    }

    // ── POST /Auth/Logout ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ── GET /Auth/AccessDenied ───────────────────────────────────────
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}