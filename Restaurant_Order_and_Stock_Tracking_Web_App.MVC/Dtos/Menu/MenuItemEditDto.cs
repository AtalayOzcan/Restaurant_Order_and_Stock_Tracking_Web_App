using Microsoft.AspNetCore.Http;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Menu;

public class MenuItemEditDto
{
    public int? Id { get; set; }

    // ── Ürün Adı (çok dilli) ─────────────────────────────────────────
    public string MenuItemName { get; set; }
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? NameRu { get; set; }

    public int CategoryId { get; set; }

    /// <summary>Fiyat string olarak gelir; Controller'da parse edilir.</summary>
    public string MenuItemPriceStr { get; set; }

    // ── Kısa Açıklama (çok dilli) ────────────────────────────────────
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionRu { get; set; }

    /// <summary>QR Menü detay sayfasında gösterilen uzun açıklama.</summary>
    public string? DetailedDescription { get; set; }

    public int StockQuantity { get; set; }
    public bool TrackStock { get; set; }
    public bool IsAvailable { get; set; }

    /// <summary>Menü listesinde gösterim sırası (1, 2, 3...). Küçük değer üstte görünür.</summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>Ürün görseli — multipart/form-data ile yüklenir.</summary>
    public IFormFile? ImageFile { get; set; }

    /// <summary>true ise mevcut görsel silinir (yeni dosya yüklenmese bile).</summary>
    public bool RemoveImage { get; set; } = false;
}