namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Shift
{
    /// <summary>Z Raporu / Vardiya detay ekranı için ViewModel</summary>
    public class ShiftDetailViewModel
    {
        public Models.ShiftLog ShiftLog { get; set; } = null!;

        // ── Ödeme dağılımı ────────────────────────────────────────────
        public decimal TotalCash { get; set; }
        public decimal TotalCreditCard { get; set; }   // PaymentsMethod == 1
        public decimal TotalDebitCard { get; set; }   // PaymentsMethod == 2
        public decimal TotalOther { get; set; }   // PaymentsMethod == 3

        // ── Zayi / Fire (StockLog'dan okunur — IsWasted ezilme problemi yok) ─
        /// <summary>
        /// İptal edilip YAKILAN (IsWasted=true) adet.
        /// Kaynak: StockLog.SourceType == "SiparişKaynaklı".
        /// Gerçek finansal kayıp.
        /// </summary>
        public int WasteCount { get; set; }

        /// <summary>
        /// İptal edilip yakılan kalemlerin finansal tutarı.
        /// Kaynak: StockLog.QuantityChange * UnitPrice.
        /// </summary>
        public decimal WasteAmount { get; set; }

        // ── Stok İade (StockLog'dan okunur) ──────────────────────────
        /// <summary>
        /// İptal edilip STOĞA İADE edilen adet (IsWasted=false).
        /// Kaynak: StockLog Note "İptal iadesi" olan Giriş hareketleri.
        /// Mali kayıp DEĞİLDİR.
        /// </summary>
        public int StockReturnCount { get; set; }

        // ── Garson bazlı satış ────────────────────────────────────────
        public List<WaiterSalesRow> WaiterSales { get; set; } = new();

        // ── Top 5 ürün ────────────────────────────────────────────────
        public List<TopProductRow> TopProducts { get; set; } = new();

        // ── Kategori dağılımı ─────────────────────────────────────────
        public List<CategorySalesRow> CategorySales { get; set; } = new();
    }

    public class WaiterSalesRow
    {
        public string WaiterName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageBasket => OrderCount > 0 ? TotalAmount / OrderCount : 0;
    }

    public class TopProductRow
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CategorySalesRow
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}