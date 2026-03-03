// ════════════════════════════════════════════════════════════════════════════
//  ViewModels/Dashboard/DashboardViewModel.cs
//  Yol: Restaurant_Order_and_Stock_Tracking_Web_App.MVC/ViewModels/Dashboard/
// ════════════════════════════════════════════════════════════════════════════

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Dashboard
{
    /// <summary>
    /// Dashboard / Ana Ekran için tüm KPI ve grafik verilerini taşıyan
    /// strongly-typed ViewModel.
    /// </summary>
    public class DashboardViewModel
    {
        // ── KPI KARTLARI ────────────────────────────────────────────────────

        /// <summary>Bugünkü toplam ciro (ödendi statüsündeki siparişler).</summary>
        public decimal DailyTotalRevenue { get; set; }

        /// <summary>
        /// Düne göre ciro değişim yüzdesi.
        /// Pozitif → artış, negatif → düşüş.
        /// null → dün veri yoktu (sıfırdan büyüme hesaplanamaz).
        /// </summary>
        public decimal? RevenueTrendPercentage { get; set; }

        /// <summary>Bugün açılan sipariş adedi (open + paid).</summary>
        public int TotalOrdersToday { get; set; }

        /// <summary>Bugünkü adisyon ortalaması (ödendi siparişler).</summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>Şu an aktif (open) sipariş adedi.</summary>
        public int ActiveOrdersNow { get; set; }

        // ── MASA DURUMU ─────────────────────────────────────────────────────

        public TableStatusSummaryData TableStatus { get; set; } = new();

        // ── GRAFİK VERİLERİ ─────────────────────────────────────────────────

        /// <summary>Saatlik sipariş/ciro yoğunluğu — Line Chart için.</summary>
        public List<HourlyTrendPoint> HourlyOrderTrends { get; set; } = new();

        /// <summary>En çok satan 5 ürün — Doughnut Chart için.</summary>
        public List<TopProductPoint> TopSellingProducts { get; set; } = new();

        // ── HIZLI NOTLAR ────────────────────────────────────────────────────

        /// <summary>Garson çağıran masa sayısı (anlık).</summary>
        public int WaiterCallsActive { get; set; }

        /// <summary>Kritik stok uyarısı olan ürün sayısı.</summary>
        public int LowStockAlerts { get; set; }
    }

    // ── YARDIMCI SINIFLAR ────────────────────────────────────────────────────

    public class TableStatusSummaryData
    {
        public int Empty { get; set; }   // TableStatus == 0
        public int Occupied { get; set; }   // TableStatus == 1
        public int Reserved { get; set; }   // TableStatus == 2
        public int Total => Empty + Occupied + Reserved;

        public decimal OccupancyRate =>
            Total > 0 ? Math.Round((decimal)Occupied / Total * 100, 1) : 0;
    }

    public class HourlyTrendPoint
    {
        /// <summary>Görünen saat etiketi — "09:00", "10:00" vb.</summary>
        public string HourLabel { get; set; } = string.Empty;

        /// <summary>O saat diliminde açılan sipariş adedi.</summary>
        public int OrderCount { get; set; }

        /// <summary>O saat diliminde ödendi statüsüne geçen sipariş cirosu.</summary>
        public decimal Revenue { get; set; }
    }

    public class TopProductPoint
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}