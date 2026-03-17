// ════════════════════════════════════════════════════════════════════════════
//  Controllers/LandingController.cs
//
//  SPRINT B — [SB-6] Onboarding UX — Firma Kodu Gösterimi
//  SPRINT 5 — [OTP-4] E-posta Doğrulama — VerifyEmail GET/POST
//
//  Kayıt akışı (OTP ile):
//    1. POST /Landing/Register → TenantOnboardingService (pasif tenant + kullanıcı)
//    2. OTP üretilir → e-posta kuyruğuna atılır
//    3. Redirect → GET /Landing/VerifyEmail?email=...&purpose=register
//    4. POST /Landing/VerifyEmail → kod doğrula → tenant + kullanıcıyı aktif et
//    5. Redirect → /Landing/Success
// ════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Onboarding;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers;

[AllowAnonymous]
public class LandingController : Controller
{
    private readonly ITenantOnboardingService _onboardingService;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RestaurantDbContext _db;
    private readonly ILogger<LandingController> _logger;

    public LandingController(
        ITenantOnboardingService onboardingService,
        IOtpService otpService,
        IEmailSender emailSender,
        UserManager<ApplicationUser> userManager,
        RestaurantDbContext db,
        ILogger<LandingController> logger)
    {
        _onboardingService = onboardingService;
        _otpService = otpService;
        _emailSender = emailSender;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    // ── GET / ─────────────────────────────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "RestaurantOS — Restoranınızı Dijitalleştirin";
        return View();
    }

    // ── GET /Landing/Register ──────────────────────────────────────────────
    public IActionResult Register()
    {
        ViewData["Title"] = "Restoranınızı Kaydedin";
        return View();
    }

    // ── POST /Landing/Register ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("RegisterPolicy")]
    public async Task<IActionResult> Register(TenantRegisterViewModel model)
    {
        ViewData["Title"] = "Restoranınızı Kaydedin";

        if (!ModelState.IsValid)
            return View(model);

        var dto = new TenantOnboardingDto(
            RestaurantName: model.RestaurantName,
            Subdomain: model.Subdomain,
            AdminUsername: model.Username,
            Password: model.Password,
            FullName: model.FullName,
            Email: model.Email,
            PhoneNumber: model.PhoneNumber
        );

        var (success, tenantId, error) = await _onboardingService.CreateTenantAsync(dto);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Kayıt sırasında bir hata oluştu.");
            return View(model);
        }

        // ── [OTP-4] OTP üret ve e-postayı kuyruğa at ──────────────────────
        var code = _otpService.Generate(model.Email, OtpPurpose.Register);

        _emailSender.EnqueueEmail(
            to: model.Email,
            subject: "RestaurantOS — E-posta Doğrulama Kodu",
            htmlBody: EmailTemplates.OtpEmail("register", code, model.RestaurantName)
        );

        _logger.LogInformation("[REGISTER] Tenant oluşturuldu (pasif). TenantId: {TenantId} | Email: {Email}",
            tenantId, model.Email);

        // TenantId'yi TempData'ya yaz — VerifyEmail'de aktifleştirme için lazım
        TempData["PendingTenantId"] = tenantId;
        TempData["PendingRestaurantName"] = model.RestaurantName;
        TempData["PendingAdminUsername"] = model.Username;

        return RedirectToAction(nameof(VerifyEmail), new { email = model.Email, purpose = "register" });
    }

    // ── GET /Landing/VerifyEmail ───────────────────────────────────────────
    public IActionResult VerifyEmail(string email, string purpose)
    {
        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Register));

        ViewBag.Email = email;
        ViewBag.Purpose = purpose;
        ViewBag.CooldownSec = _otpService.GetCooldownSeconds(email,
            purpose == "register" ? OtpPurpose.Register : OtpPurpose.ResetPassword);

        ViewData["Title"] = "E-posta Doğrulama";
        return View();
    }

    // ── POST /Landing/VerifyEmail ──────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OtpVerifyPolicy")]
    public async Task<IActionResult> VerifyEmail(string email, string purpose, string code)
    {
        var otpPurpose = purpose == "register" ? OtpPurpose.Register : OtpPurpose.ResetPassword;

        var result = _otpService.Verify(email, otpPurpose, code?.Trim() ?? "");

        if (result == OtpVerifyResult.Success)
        {
            // ── Tenant ve kullanıcıyı aktif et ────────────────────────────
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.EmailConfirmed);

            if (user != null)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                // Tenant'ı aktif et
                var tenant = await _db.Tenants.FindAsync(user.TenantId);
                if (tenant != null)
                {
                    tenant.IsActive = true;
                    await _db.SaveChangesAsync();
                }

                _otpService.Consume(email, otpPurpose);

                _logger.LogInformation("[OTP] Kayıt doğrulandı. Email: {Email} | TenantId: {TenantId}",
                    email, user.TenantId);
            }

            // Success sayfasına yönlendir
            var tenantId = TempData["PendingTenantId"] as string ?? "";
            var restaurantName = TempData["PendingRestaurantName"] as string ?? "";
            var adminUsername = TempData["PendingAdminUsername"] as string ?? "";

            TempData["FirmaKodu"] = tenantId;
            TempData["RestaurantName"] = restaurantName;
            TempData["AdminUsername"] = adminUsername;

            return RedirectToAction(nameof(Success));
        }

        // ── Hata mesajları ─────────────────────────────────────────────────
        ViewBag.Email = email;
        ViewBag.Purpose = purpose;
        ViewBag.CooldownSec = _otpService.GetCooldownSeconds(email, otpPurpose);
        ViewData["Title"] = "E-posta Doğrulama";

        ViewBag.Error = result switch
        {
            OtpVerifyResult.InvalidCode => $"Hatalı kod. {3 - GetAttemptHint(email, otpPurpose)} deneme hakkınız kaldı.",
            OtpVerifyResult.Locked => "Çok fazla hatalı deneme. Lütfen 15 dakika bekleyin veya yeni kod isteyin.",
            OtpVerifyResult.Expired => "Kodun süresi dolmuş. Lütfen yeni kod isteyin.",
            OtpVerifyResult.NotFound => "Geçerli bir kod bulunamadı. Lütfen yeni kod isteyin.",
            _ => "Doğrulama başarısız."
        };

        return View();
    }

    // ── POST /Landing/ResendOtp ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OtpVerifyPolicy")]
    public IActionResult ResendOtp(string email, string purpose)
    {
        var otpPurpose = purpose == "register" ? OtpPurpose.Register : OtpPurpose.ResetPassword;

        if (_otpService.IsCooldownActive(email, otpPurpose))
        {
            TempData["ResendError"] = "Lütfen cooldown süresinin bitmesini bekleyin.";
            return RedirectToAction(nameof(VerifyEmail), new { email, purpose });
        }

        var code = _otpService.Generate(email, otpPurpose);

        _emailSender.EnqueueEmail(
            to: email,
            subject: "RestaurantOS — Yeni Doğrulama Kodu",
            htmlBody: EmailTemplates.OtpEmail(purpose, code)
        );

        _logger.LogInformation("[OTP] Yeniden gönderildi. Email: {Email} | Purpose: {Purpose}", email, purpose);

        TempData["ResendSuccess"] = "Yeni kod gönderildi.";
        return RedirectToAction(nameof(VerifyEmail), new { email, purpose });
    }

    // ── GET /Landing/Success ───────────────────────────────────────────────
    public IActionResult Success()
    {
        if (TempData["FirmaKodu"] is not string firmaKodu || string.IsNullOrEmpty(firmaKodu))
            return RedirectToAction(nameof(Register));

        ViewData["Title"] = "Restoranınız Oluşturuldu 🎉";
        ViewBag.FirmaKodu = firmaKodu;
        ViewBag.RestaurantName = TempData["RestaurantName"] as string ?? "";
        ViewBag.AdminUsername = TempData["AdminUsername"] as string ?? "";

        return View();
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────
    private int GetAttemptHint(string email, OtpPurpose purpose)
    {
        // AttemptCount'a direkt erişim yok — sadece UI hint için yaklaşık değer
        // Gerçek lockout IOtpService.Verify içinde yapılıyor
        return 0;
    }
}