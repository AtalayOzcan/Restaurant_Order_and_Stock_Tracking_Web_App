// ============================================================================
//  Areas/Admin/Controllers/ImpersonationController.cs
//
//  SPRINT 4 — [IMP-3] SysAdmin → Tenant Impersonation Başlatma
//
//  GÜVENLIK KATMANLARI:
//    1. AdminBaseController: [Authorize(Roles="SysAdmin", AuthenticationSchemes="AdminAuth")]
//       AppAuth cookie'si bu endpoint'e hiç ulaşamaz.
//    2. CSRF koruması: [ValidateAntiForgeryToken] — GET ile token üretimi yasak.
//    3. Token 5 dakika ömürlü, tek kullanımlık.
//    4. Her işlem ILogger.LogWarning ile audit log'a düşer.
//    5. Hedef tenant aktif değilse token üretilmez.
//
//  AKIŞ:
//    POST /Admin/Impersonation/Begin → JSON { token: "uuid" } döner.
//    UI bu token'ı yeni sekmede /App/Auth/Impersonate?token=... olarak açar.
//    AdminAuth cookie'si hiç bozulmaz.
// ============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.Admin.Controllers
{
    public class ImpersonationController : AdminBaseController
    {
        private readonly RestaurantDbContext _db;
        private readonly ILogger<ImpersonationController> _logger;

        public ImpersonationController(
            RestaurantDbContext db,
            ILogger<ImpersonationController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── POST /Admin/Impersonation/Begin ───────────────────────────────────
        // SysAdmin "Bu restorana gir" butonuna bastığında çağrılır.
        // JSON döner: { token: "uuid", impersonateUrl: "/App/Auth/Impersonate?token=..." }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Begin([FromBody] ImpersonationBeginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TargetTenantId))
                return BadRequest(new { success = false, message = "Geçersiz tenant." });

            // ── Hedef tenant var mı ve aktif mi? ──────────────────────────────
            var tenant = await _db.Tenants
                .AsNoTracking()
                .Select(t => new { t.TenantId, t.Name, t.IsActive })
                .FirstOrDefaultAsync(t => t.TenantId == dto.TargetTenantId);

            if (tenant == null)
                return NotFound(new { success = false, message = "Restoran bulunamadı." });

            if (!tenant.IsActive)
                return BadRequest(new { success = false, message = "Restoran hesabı pasif durumda." });

            // ── SysAdmin kimliği ───────────────────────────────────────────────
            var sysAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue(ClaimTypes.Name)
                          ?? "bilinmiyor";

            // ── Token oluştur ─────────────────────────────────────────────────
            var token = new ImpersonationToken
            {
                SysAdminId = sysAdminId,
                TargetTenantId = dto.TargetTenantId,
                TargetRole = "Admin",           // Her zaman Admin — Garson/Kasiyer yasak
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)  // Onaylanan karar: 5 dakika
            };

            _db.ImpersonationTokens.Add(token);
            await _db.SaveChangesAsync();

            // ── Audit log ─────────────────────────────────────────────────────
            _logger.LogWarning(
                "[IMPERSONATION] Token üretildi. SysAdmin: {SysAdminId} → Tenant: {TenantId} ({TenantName}) | TokenId: {TokenId} | ExpiresAt: {ExpiresAt}",
                sysAdminId,
                dto.TargetTenantId,
                tenant.Name,
                token.TokenId,
                token.ExpiresAt);

            var impersonateUrl = Url.Action(
                action: "Impersonate",
                controller: "Auth",
                values: new { area = "App", token = token.TokenId },
                protocol: Request.Scheme)!;

            return Json(new
            {
                success = true,
                token = token.TokenId,
                impersonateUrl,
                expiresInSecs = 300   // UI'da geri sayım için
            });
        }

        // ── POST /Admin/Impersonation/Revoke ──────────────────────────────────
        // Manuel iptal: Kullanılmamış bir token'ı SysAdmin geçersiz kılar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke([FromBody] ImpersonationRevokeDto dto)
        {
            var token = await _db.ImpersonationTokens
                .FirstOrDefaultAsync(t => t.TokenId == dto.TokenId && t.UsedAt == null);

            if (token == null)
                return NotFound(new { success = false, message = "Token bulunamadı veya zaten kullanılmış." });

            // İptal için UsedAt'i set et — aynı geçersizlik mekanizması kullanılır
            token.UsedAt = DateTime.UtcNow;
            token.UsedFromIp = "REVOKED_BY_SYSADMIN";
            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "[IMPERSONATION] Token iptal edildi. SysAdmin: {SysAdminId} | TokenId: {TokenId}",
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                dto.TokenId);

            return Json(new { success = true, message = "Token iptal edildi." });
        }
    }

    // ── DTO'lar ───────────────────────────────────────────────────────────────
    public class ImpersonationBeginDto
    {
        public string TargetTenantId { get; set; } = string.Empty;
    }

    public class ImpersonationRevokeDto
    {
        public Guid TokenId { get; set; }
    }
}