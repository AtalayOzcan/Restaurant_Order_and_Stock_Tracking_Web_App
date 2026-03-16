// ============================================================================
//  Models/ImpersonationToken.cs
//
//  SPRINT 4 — [IMP-1] Impersonation Token Modeli
//
//  Güvenlik garantileri:
//    • Tek kullanımlık: UsedAt != null ise geçersiz.
//    • 5 dakika ömür: ExpiresAt < NOW() ise geçersiz.
//    • Atomik tüketim: AuthController.Impersonate() içinde
//      UPDATE ... WHERE used_at IS NULL AND expires_at > NOW() RETURNING *
//      ile race condition imkânsız kılınır.
//    • Audit trail: SysAdminId, TargetTenantId, UsedAt, UsedFromIp
//      her impersonation olayını takip eder.
//    • Temizlik: ReservationCleanupService her gece
//      expires_at < NOW() - 7gün olan kayıtları siler.
// ============================================================================

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    public class ImpersonationToken
    {
        // ── Kimlik ───────────────────────────────────────────────────────────
        /// <summary>UUID primary key — URL'de taşınır.</summary>
        public Guid TokenId { get; set; } = Guid.NewGuid();

        // ── Kim üretiyor ─────────────────────────────────────────────────────
        /// <summary>Token'ı üreten SysAdmin'in ApplicationUser.Id değeri.</summary>
        public string SysAdminId { get; set; } = string.Empty;

        // ── Hedef ────────────────────────────────────────────────────────────
        /// <summary>Hangi restoran/tenant'a giriş yapılacak.</summary>
        public string TargetTenantId { get; set; } = string.Empty;

        /// <summary>
        /// Hangi rolle giriş yapılacak.
        /// Güvenlik kuralı: SysAdmin sadece "Admin" rolüyle girebilir.
        /// Garson veya Kasiyer rolüyle impersonation yasak.
        /// </summary>
        public string TargetRole { get; set; } = "Admin";

        // ── Zaman ────────────────────────────────────────────────────────────
        /// <summary>Token üretim zamanı (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Token geçerlilik sonu (UTC).
        /// CreatedAt + 5 dakika olarak set edilir.
        /// Bu süreden sonra atomik UPDATE sorgusunda expires_at &gt; NOW()
        /// koşulu geçmez → token otomatik reddedilir.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        // ── Kullanım kaydı ───────────────────────────────────────────────────
        /// <summary>
        /// Token tüketildiğinde set edilir.
        /// NULL ise henüz kullanılmamış.
        /// NOT NULL ise tek kullanımlık kural gereği artık geçersiz.
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Token'ı kullanan tarayıcının IP adresi.
        /// Audit trail için — yasal uyum ve güvenlik incelemesinde kullanılır.
        /// </summary>
        public string? UsedFromIp { get; set; }
    }
}