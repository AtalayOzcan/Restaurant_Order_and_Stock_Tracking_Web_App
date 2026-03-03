namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Menu;

public class MenuItemEditDto
{
    public int? Id { get; set; }
    public string MenuItemName { get; set; }
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? NameRu { get; set; }
    public int CategoryId { get; set; }
    public string MenuItemPriceStr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionRu { get; set; }
    public string? DetailedDescription { get; set; }
    public int StockQuantity { get; set; }
    public bool TrackStock { get; set; }
    public bool IsAvailable { get; set; }
}