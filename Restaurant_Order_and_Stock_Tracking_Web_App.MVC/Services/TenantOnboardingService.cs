// ============================================================================
//  Services/TenantOnboardingService.cs
//
//  SPRINT C DEĞİŞİKLİKLERİ:
//  [SC-5] Adım 2'ye telefon tekrarı kontrolü eklendi (PostgreSQL uyumlu EF Core sorgu)
//  [SC-6] AdminUser oluşturmada PhoneNumber alanı set ediliyor
// ============================================================================

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services;

public class TenantOnboardingService : ITenantOnboardingService
{
    private readonly RestaurantDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public TenantOnboardingService(
        RestaurantDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<(bool Success, string? TenantId, string? Error)> CreateTenantAsync(
        TenantOnboardingDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // ── Adım 1: TenantId slug üret ────────────────────────────────
            var baseSlug = GenerateSlug(dto.RestaurantName);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantId = $"{baseSlug}-{suffix}";

            // ── Adım 2: Benzersizlik kontrolleri ──────────────────────────

            // Subdomain
            var subdomainExists = await _db.Tenants
                .AnyAsync(t => t.Subdomain == dto.Subdomain);
            if (subdomainExists)
                return (false, null, $"'{dto.Subdomain}' subdomain'i zaten kullanımda. Farklı bir subdomain seçin.");

            // Kullanıcı adı
            if (await _userManager.FindByNameAsync(dto.AdminUsername) != null)
                return (false, null, $"'{dto.AdminUsername}' kullanıcı adı zaten alınmış. Farklı bir kullanıcı adı seçin.");

            // [SC-5] Telefon numarası tekrarı — Trial koruması
            // IdentityUser.PhoneNumber sütununa PostgreSQL'de case-sensitive eşleşme yeterli.
            // EF Core → Npgsql bu sorguyu parameterized olarak üretir; SQL injection riski yok.
            var phoneAlreadyUsed = await _db.Users
                .AnyAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (phoneAlreadyUsed)
                return (false, null, "Bu telefon numarası ile zaten bir deneme sürümü kullanılmış.");

            // ── Adım 3: Admin rolünü güvence altına al ───────────────────
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            // ── Adım 4: Tenant kaydı ──────────────────────────────────────
            var tenant = new Tenant
            {
                TenantId = tenantId,
                Name = dto.RestaurantName,
                Subdomain = dto.Subdomain,
                PlanType = "trial",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TrialEndsAt = DateTime.UtcNow.AddDays(30),
                RestaurantType = RestaurantType.CasualDining,
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            // ── Adım 5: TenantConfig ──────────────────────────────────────
            var config = new TenantConfig
            {
                TenantId = tenantId,
                EnableKitchenDisplay = true,
                EnableReservations = true,
                EnableDiscounts = true,
                EnableTableMerge = false,
                EnableSelfOrderQr = false,
                CurrencyCode = "TRY",
            };
            _db.TenantConfigs.Add(config);
            await _db.SaveChangesAsync();

            // ── Adım 6: Admin kullanıcısı ─────────────────────────────────
            var adminUser = new ApplicationUser
            {
                UserName = $"{tenantId}_{dto.AdminUsername.Trim()}",
                FullName = dto.FullName,
                Email = dto.Email,
                EmailConfirmed = true,
                PhoneNumber = dto.PhoneNumber,   // [SC-6] Trial koruması için kayıt
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId,
            };

            var createResult = await _userManager.CreateAsync(adminUser, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                await tx.RollbackAsync();
                return (false, null, $"Kullanıcı oluşturulamadı: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                await tx.RollbackAsync();
                return (false, null, $"Rol ataması başarısız: {errors}");
            }

            // ── Adım 7: Commit ────────────────────────────────────────────
            await tx.CommitAsync();
            return (true, tenantId, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, null, $"Kayıt sırasında beklenmeyen hata oluştu: {ex.Message}");
        }
    }

    // ── Yardımcı: Restoran adından URL-safe slug üret ─────────────────────
    private static string GenerateSlug(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = sb.ToString()
            .Replace('ı', 'i').Replace('İ', 'i')
            .Replace('ğ', 'g').Replace('Ğ', 'g')
            .Replace('ş', 's').Replace('Ş', 's')
            .Replace('ö', 'o').Replace('Ö', 'o')
            .Replace('ü', 'u').Replace('Ü', 'u')
            .Replace('ç', 'c').Replace('Ç', 'c');

        result = result.ToLowerInvariant();
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
        result = Regex.Replace(result, @"\s+", "-");
        result = Regex.Replace(result, @"-+", "-");
        result = result.Trim('-');

        if (string.IsNullOrEmpty(result)) result = "restoran";
        if (result.Length > 40) result = result[..40].TrimEnd('-');

        return result;
    }
}