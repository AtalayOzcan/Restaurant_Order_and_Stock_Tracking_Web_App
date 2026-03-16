// ============================================================================
//  Services/IStockService.cs
//
//  Stok yönetimi servis sözleşmesi.
//
//  NEDEN SERVIS KATMANI?
//  ─────────────────────────────────────────────────────────────────────────
//  StockController.UpdateStock() içinde 80 satır if/else iş mantığı
//  (direct / fire / movement modları, miktar hesaplama, StockLog yazımı)
//  doğrudan controller'da bulunuyordu:
//    - Test edilemez (HTTP katmanına sıkı bağımlı)
//    - Controller Thin Controller ilkesini ihlal ediyor
//    - Aynı stok hesaplama mantığı başka yerden çağrılamıyor
//
//  YENİ DURUM:
//    StockController.UpdateStock() → IStockService.UpdateStockAsync() çağırır
//    Tüm iş mantığı StockService içinde, transaction korumasında çalışır.
//
//  ServiceResult: Modules/Orders/IOrderService.cs içinde tanımlı
//    record ServiceResult(bool Success, string Message)
//    record ServiceResult<T>(bool Success, string Message, T? Data)
//
//  UpdateStockResult: UI'a dönecek JSON verisi (newStock, statusLabel vb.)
//    StockController bu data'yı alıp doğrudan Json() ile döndürür.
// ============================================================================

using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Stock;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Modules.Orders;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services
{
    /// <summary>
    /// UpdateStock işleminden dönen UI verisi.
    /// Controller bu nesneyi alır, doğrudan Json() ile döndürür.
    /// </summary>
    public record UpdateStockResult(
        int NewStock,
        string Status,
        string StatusLabel,
        string StatusPill,
        int AlertThreshold,
        int CriticalThreshold
    );

    public interface IStockService
    {
        /// <summary>
        /// Stok güncelleme işlemi: direct / fire / movement modlarını destekler.
        /// MenuItem stok değerini günceller, StockLog kaydı oluşturur.
        /// İşlem transaction bloğunda güvenle çalışır.
        /// </summary>
        /// <param name="dto">UI'dan gelen güncelleme verisi.</param>
        /// <param name="tenantId">Cross-tenant güvenlik kontrolü için.</param>
        Task<ServiceResult<UpdateStockResult>> UpdateStockAsync(
            StockUpdateDto dto,
            string tenantId);
    }
}