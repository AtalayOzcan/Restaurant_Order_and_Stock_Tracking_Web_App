namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    /// <summary>
    /// Stok hareket geçmişi tablosu.
    /// Her stok değişikliğinde (Giriş / Çıkış / Düzeltme) otomatik kayıt düşülür.
    /// </summary>
    public class StockLog
    {
        public int StockLogId { get; set; }

        public int MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; }

        /// <summary>"Giriş" | "Çıkış" | "Düzeltme"</summary>
        public string MovementType { get; set; }

        /// <summary>Giriş → pozitif, Çıkış → negatif, Düzeltme → fark (+/-)</summary>
        public int QuantityChange { get; set; }

        public int PreviousStock { get; set; }
        public int NewStock { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}