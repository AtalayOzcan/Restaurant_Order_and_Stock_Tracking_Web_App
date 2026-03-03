using Microsoft.EntityFrameworkCore.Migrations;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public string MenuItemName { get; set; }   // TR (mevcut)
        public string? NameEn { get; set; }   // EN
        public string? NameAr { get; set; }   // AR
        public string? NameRu { get; set; }   // RU

        public decimal MenuItemPrice { get; set; }
        public decimal? CostPrice { get; set; } = null;

        public int AlertThreshold { get; set; } = 0;
        public int CriticalThreshold { get; set; } = 0;
        public int StockQuantity { get; set; }
        public bool TrackStock { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsDeleted { get; set; } = false;

        public string? Description { get; set; }   // TR (mevcut)
        public string? DescriptionEn { get; set; }   // EN
        public string? DescriptionAr { get; set; }   // AR
        public string? DescriptionRu { get; set; }   // RU

        public DateTime MenuItemCreatedTime { get; set; }
    }
}
