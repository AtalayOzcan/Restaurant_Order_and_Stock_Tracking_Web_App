using System.Security.Claims;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Interface
    // ──────────────────────────────────────────────────────────────────────────
    public interface ICurrentUserService
    {
        /// <summary>
        /// Log'a yazılacak kullanıcı adı.
        /// Impersonation aktifse "[SysAdmin:{id}] → {tenantUserName}" formatında.
        /// Normal oturumda sadece kullanıcı adı döner.
        /// </summary>
        string DisplayName { get; }

        /// <summary>Tenant ID — Global Query Filter ile uyumlu.</summary>
        string? TenantId { get; }

        /// <summary>Oturum bir SysAdmin impersonation'ı mı?</summary>
        bool IsImpersonating { get; }

        /// <summary>
        /// Impersonation aktifse bu session'ı başlatan SysAdmin'in ID'si.
        /// Normal oturumda null.
        /// </summary>
        string? ImpersonatedBy { get; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Implementasyon
    // ──────────────────────────────────────────────────────────────────────────
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User
            => _httpContextAccessor.HttpContext?.User;

        public string? TenantId
            => User?.FindFirstValue("TenantId");

        public bool IsImpersonating
            => User?.FindFirstValue("IsImpersonation") == "true";

        public string? ImpersonatedBy
            => IsImpersonating ? User?.FindFirstValue("ImpersonatedBy") : null;

        public string DisplayName
        {
            get
            {
                // Kullanıcı adını hesapla
                var name = User?.FindFirstValue("FullName")
                        ?? User?.FindFirstValue(ClaimTypes.Name)
                        ?? "Bilinmiyor";

                if (!IsImpersonating)
                    return name;

                // Impersonation aktif → audit prefix ekle
                // Format: "[SysAdmin:{id}] → {tenantUserName}"
                // Bu format log aggregator'larında (Seq, Serilog) filtrelenebilir.
                var sysAdminId = ImpersonatedBy ?? "?";
                return $"[SysAdmin:{sysAdminId}] → {name}";
            }
        }
    }
}