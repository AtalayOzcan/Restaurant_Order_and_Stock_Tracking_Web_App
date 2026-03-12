// ============================================================================
//  Services/ITenantOnboardingService.cs
//
//  SPRINT C DEĞİŞİKLİKLERİ:
//  [SC-5] TenantOnboardingDto → PhoneNumber alanı eklendi
// ============================================================================

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services;

public record TenantOnboardingDto(
    string RestaurantName,
    string Subdomain,
    string AdminUsername,
    string Password,
    string FullName,
    string? Email,
    string PhoneNumber       // [SC-5] Trial koruması için zorunlu
);

public interface ITenantOnboardingService
{
    Task<(bool Success, string? TenantId, string? Error)> CreateTenantAsync(TenantOnboardingDto dto);
}