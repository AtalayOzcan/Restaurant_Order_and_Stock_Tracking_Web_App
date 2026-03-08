// ============================================================================
//  Shared/Common/Enums.cs
//  FAZ 1 FİNAL — Teknik Borç: String → Enum Geçişi
//
//  NEDEN ENUM?
//  ───────────
//  Eski hâlde OrderStatus ve OrderItemStatus string alanlarıydı.
//  Bu; yanlış yazım ("Open" vs "open"), compile-time güvence eksikliği
//  ve switch/if zincirlerinde kırılganlık anlamına geliyordu.
//
//  NEDEN VALUE CONVERTER?
//  ─────────────────────
//  KDS (KitchenController) ve QR Menü frontend'leri JavaScript tarafında
//  'pending', 'open' gibi küçük harfli string'ler bekliyor.
//  Value Converter sayesinde:
//    C# tarafı  → Enum  (type-safe, IntelliSense, switch pattern)
//    DB tarafı  → string ("open", "pending" — JS frontend mutlu)
//    EF LINQ    → Enum karşılaştırmaları doğru SQL string'e dönüşür
//
//  MAPPING (lowercase):
//    OrderStatus.Open        ↔ "open"
//    OrderStatus.Paid        ↔ "paid"
//    OrderStatus.Cancelled   ↔ "cancelled"
//
//    OrderItemStatus.Pending    ↔ "pending"
//    OrderItemStatus.Preparing  ↔ "preparing"
//    OrderItemStatus.Ready      ↔ "ready"
//    OrderItemStatus.Served     ↔ "served"
//    OrderItemStatus.Cancelled  ↔ "cancelled"
// ============================================================================

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Shared.Common
{
    /// <summary>
    /// Adisyon (sipariş) durum enum'u.
    /// DB'ye küçük harfli string olarak yazılır (Value Converter — RestaurantDbContext).
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Adisyon açık, henüz kapanmadı. DB: "open"</summary>
        Open,

        /// <summary>Ödeme tamamlandı, adisyon kapatıldı. DB: "paid"</summary>
        Paid,

        /// <summary>İptal edildi (sıfır tutarlı ya da yönetici tarafından). DB: "cancelled"</summary>
        Cancelled
    }

    /// <summary>
    /// Sipariş kalemi (OrderItem) durum enum'u.
    /// DB'ye küçük harfli string olarak yazılır (Value Converter — RestaurantDbContext).
    /// KDS (Mutfak Ekranı) bu değerlere göre kalemler arası geçiş yapar:
    ///   Pending → Preparing → Ready → Served
    /// </summary>
    public enum OrderItemStatus
    {
        /// <summary>Yeni eklendi, mutfakta henüz işlem görmedi. DB: "pending"</summary>
        Pending,

        /// <summary>Mutfakta hazırlanıyor. DB: "preparing"</summary>
        Preparing,

        /// <summary>Hazır, servise çıkmaya hazır. DB: "ready"</summary>
        Ready,

        /// <summary>Masaya servis edildi. DB: "served"</summary>
        Served,

        /// <summary>İptal edildi (kısmi veya tam). DB: "cancelled"</summary>
        Cancelled
    }

    // ═══════════════════════════════════════════════════════════════════════
    // [F-02] StockLog Hareket Kategorisi — Note.StartsWith fragility ortadan kaldırıldı
    //
    // ESKİ: StockLog.Note "İptal iadesi — ..." ile başlıyor mu diye string match.
    //       Yazım hatası / dil değişimi / refactor → rapor bozuluyor.
    // YENİ: DB'de integer kolona yazılan enum → type-safe, refactor-safe, hızlı filtre.
    //
    // DB default değeri: 0 = Manual
    // ═══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// StockLog hareketi kategorisi — hangi süreçten kaynaklandığını tanımlar.
    /// DB'ye integer olarak yazılır (value converter yok; EF Core doğrudan int saklar).
    /// </summary>
    public enum MovementCategory
    {
        /// <summary>El ile yapılan stok girişi / düzeltmesi. DB: 0 (default)</summary>
        Manual = 0,

        /// <summary>
        /// Sipariş iptali sonrası stoka iade.
        /// CancelItemAsync → IsWasted = false durumunda yazılır.
        /// </summary>
        ReturnFromCancel = 1,

        /// <summary>
        /// Sipariş kaynaklı zayi/fire (ürün kullanıldı, stoka dönmez).
        /// CancelItemAsync → IsWasted = true durumunda yazılır.
        /// </summary>
        OrderWaste = 2,

        /// <summary>
        /// Stok kaynaklı fire (depo kırık/bozuk).
        /// StockController.UpdateStock fire modunda yazılır.
        /// </summary>
        StockWaste = 3
    }
}