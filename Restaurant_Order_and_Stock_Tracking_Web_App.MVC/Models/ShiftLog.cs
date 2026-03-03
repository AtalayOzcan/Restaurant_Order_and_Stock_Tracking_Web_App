namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    /// <summary>
    /// Kasa vardiyası kaydı. Her açık vardiya için bir satır tutulur.
    /// </summary>
    public class ShiftLog
    {
        public int ShiftLogId { get; set; }

        // ── Zaman ───────────────────────────────────────────────────
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // ── Kullanıcılar ─────────────────────────────────────────────
        public string OpenedByUserId { get; set; } = string.Empty;
        public string? ClosedByUserId { get; set; }

        public virtual ApplicationUser OpenedByUser { get; set; } = null!;
        public virtual ApplicationUser? ClosedByUser { get; set; }

        // ── Nakit Sayımları ──────────────────────────────────────────
        public decimal OpeningBalance { get; set; }    // Gün başı nakit sayımı
        public decimal ClosingBalance { get; set; }    // Kapanıştaki nakit sayımı (manuel giriş)

        // ── Sistem Hesaplanan Toplamlar ──────────────────────────────
        public decimal TotalSales { get; set; }        // Sistem toplamı (hesaplanan)
        public decimal TotalCash { get; set; }         // Nakit ödemeler toplamı
        public decimal TotalCard { get; set; }         // Kart (kredi+banka) toplamı
        public decimal TotalOther { get; set; }        // Yemek çeki / diğer
        public decimal TotalDiscount { get; set; }     // İndirim toplamı
        public decimal TotalWaste { get; set; }        // İptal/zayi tutarı
        public decimal Difference { get; set; }        // ClosingBalance - (OpeningBalance + TotalCash)

        // ── Durum / Notlar ───────────────────────────────────────────
        public string? Notes { get; set; }

        public bool IsClosed { get; set; } = false;    // false=açık, true=kapalı
        public bool IsLocked { get; set; } = false;    // Admin kilidi

        public decimal DifferenceThreshold { get; set; } = 100m; // Uyarı eşiği (₺)
    }
}
