namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }       // TR (mevcut)
    public string? NameEn { get; set; }       // EN
    public string? NameAr { get; set; }       // AR
    public string? NameRu { get; set; }       // RU
    public int CategorySortOrder { get; set; }
    public bool IsActive { get; set; }

    public virtual ICollection<MenuItem> MenuItems { get; set; }
}