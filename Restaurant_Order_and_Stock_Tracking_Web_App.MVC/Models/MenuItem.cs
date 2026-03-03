namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // ── Ürün Adı (çok dilli) ─────────────────────────────────────
        public string MenuItemName { get; set; }   // TR (zorunlu)
        public string? NameEn { get; set; }         // EN
        public string? NameAr { get; set; }         // AR
        public string? NameRu { get; set; }         // RU

        public decimal MenuItemPrice { get; set; }
        public decimal? CostPrice { get; set; } = null;

        public int AlertThreshold { get; set; } = 0;
        public int CriticalThreshold { get; set; } = 0;
        public int StockQuantity { get; set; }
        public bool TrackStock { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsDeleted { get; set; } = false;

        // ── Kısa Açıklama (çok dilli) ────────────────────────────────
        public string? Description { get; set; }     // TR (mevcut)
        public string? DescriptionEn { get; set; }   // EN
        public string? DescriptionAr { get; set; }   // AR
        public string? DescriptionRu { get; set; }   // RU

        /// <summary>QR Menü detay sayfasında gösterilen uzun açıklama.</summary>
        public string? DetailedDescription { get; set; }

        /// <summary>wwwroot göreli yolu, ör: /images/menu/abc123.jpg</summary>
        public string? ImagePath { get; set; }

        public DateTime MenuItemCreatedTime { get; set; }
    }
}
