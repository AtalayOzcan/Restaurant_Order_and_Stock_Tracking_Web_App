// ============================================================================
//  Services/StockService.cs
//
//  REFACTORING: StockController.UpdateStock() → StockService
//
//  ESKİ DURUM (kaldırıldı):
//    StockController.UpdateStock() → 80 satır if/else iş mantığı
//    → direct / fire / movement modları controller'da
//    → StockLog yazımı controller'da
//    → SaveChangesAsync() transaction korumasız
//
//  YENİ DURUM:
//    StockService.UpdateStockAsync() → tüm iş mantığı burada
//    → Transaction bloğu ile güvenli kayıt
//    → Test edilebilir (DbContext mock'lanabilir)
//    → StockController 8 satıra indi
//
//  MODLAR:
//    "direct"   → Direkt stok değeri girişi (sayım/düzeltme)
//                 MovementCategory.Manual, MovementType="Düzeltme"
//    "fire"     → Stok kaynaklı fire/zayi (depo kırık/bozuk)
//                 MovementCategory.StockWaste, MovementType="Çıkış"
//                 SourceType="StokKaynaklı", Note zorunlu
//    "movement" → Hareket bazlı giriş/çıkış (mal girişi, normal çıkış)
//                 MovementCategory.Manual, MovementType="Giriş"/"Çıkış"
//                 Note zorunlu
//
//  GÜVENLİK:
//    tenantId parametresi ile cross-tenant erişim kontrolü yapılır.
//    (StockController [Authorize(Roles="Admin")] ile de korunuyor,
//     bu ikinci savunma hattıdır.)
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Stock;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Modules.Orders;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Shared.Common;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services
{
    public class StockService : IStockService
    {
        private readonly RestaurantDbContext _context;

        public StockService(RestaurantDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        //  UpdateStockAsync
        //  direct / fire / movement modlarını destekler.
        //  Transaction bloğu içinde MenuItem + StockLog birlikte kaydedilir.
        // =====================================================================
        public async Task<ServiceResult<UpdateStockResult>> UpdateStockAsync(
            StockUpdateDto dto,
            string tenantId)
        {
            // =================================================================
            //  1. Ürünü bul ve cross-tenant kontrol yap
            // =================================================================
            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == dto.MenuItemId);

            if (item == null)
                return new(false, "Ürün bulunamadı.");

            if (item.TenantId != tenantId)
                return new(false, "Bu ürüne erişim yetkiniz yok.");

            // =================================================================
            //  2. Moda göre miktar ve hareket tipi hesapla
            // =================================================================
            int previousStock;
            int newStock;
            int quantityChange;
            string movementType;
            MovementCategory movementCategory;

            previousStock = item.StockQuantity;

            if (dto.UpdateMode == "direct")
            {
                // ── Direkt stok değeri girişi (sayım düzeltmesi) ─────────────
                if (dto.NewStockValue == null || dto.NewStockValue < 0)
                    return new(false, "Geçerli bir stok değeri giriniz.");

                newStock = dto.NewStockValue.Value;
                quantityChange = newStock - previousStock;
                movementType = "Düzeltme";
                movementCategory = MovementCategory.Manual;
            }
            else if (dto.UpdateMode == "fire")
            {
                // ── Stok Kaynaklı Fire / Zayi (depo kırık/bozuk) ────────────
                // SourceType="StokKaynaklı" → fire raporunda ayrışır.
                // MovementDirection her zaman "out", Note zorunludur.
                if (dto.MovementQuantity == null || dto.MovementQuantity <= 0)
                    return new(false, "Fire miktarını giriniz.");

                if (string.IsNullOrWhiteSpace(dto.Note))
                    return new(false, "Fire nedenini açıklamak zorunludur (örn: 'Kırık şişeler').");

                quantityChange = -dto.MovementQuantity.Value;
                movementType = "Çıkış";
                movementCategory = MovementCategory.StockWaste;
                newStock = previousStock + quantityChange;

                if (newStock < 0)
                    return new(false, $"Stok sıfırın altına düşemez. Mevcut stok: {previousStock}");
            }
            else
            {
                // ── Hareket bazlı giriş/çıkış (normal mal girişi / çıkışı) ──
                if (dto.MovementQuantity == null || dto.MovementQuantity <= 0)
                    return new(false, "Geçerli bir miktar giriniz.");

                if (string.IsNullOrWhiteSpace(dto.Note))
                    return new(false, "Hareket bazlı işlem için açıklama zorunludur.");

                if (dto.MovementDirection == "in")
                {
                    quantityChange = dto.MovementQuantity.Value;
                    movementType = "Giriş";
                }
                else
                {
                    quantityChange = -dto.MovementQuantity.Value;
                    movementType = "Çıkış";
                }

                movementCategory = MovementCategory.Manual;
                newStock = previousStock + quantityChange;

                if (newStock < 0)
                    return new(false, "Stok miktarı sıfırın altına düşemez.");
            }

            // =================================================================
            //  3. Eşik değerlerini güncelle (varsa)
            // =================================================================
            if (dto.AlertThreshold.HasValue && dto.AlertThreshold.Value >= 0)
                item.AlertThreshold = dto.AlertThreshold.Value;

            if (dto.CriticalThreshold.HasValue && dto.CriticalThreshold.Value >= 0)
                item.CriticalThreshold = dto.CriticalThreshold.Value;

            // =================================================================
            //  4. MenuItem stok değerini güncelle
            // =================================================================
            item.StockQuantity = newStock;

            // =================================================================
            //  5. StockLog kaydı oluştur
            //     Transaction bloğu: MenuItem + StockLog birlikte commit edilir.
            //     Biri başarısız olursa ikisi de rollback olur.
            // =================================================================
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.StockLogs.Add(new StockLog
                {
                    MenuItemId = item.MenuItemId,
                    MovementType = movementType,
                    QuantityChange = quantityChange,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Note = dto.Note?.Trim(),
                    SourceType = dto.UpdateMode == "fire" ? "StokKaynaklı" : null,
                    OrderId = null,    // stok hareketi — adisyon bağlantısı yok
                    UnitPrice = item.MenuItemPrice,
                    CreatedAt = DateTime.UtcNow,
                    MovementCategory = movementCategory
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new(false, "Stok kaydedilirken hata oluştu: " + ex.Message);
            }

            // =================================================================
            //  6. UI'a dönecek sonuç nesnesini oluştur
            // =================================================================
            var result = new UpdateStockResult(
                NewStock: newStock,
                Status: GetStatusString(item),
                StatusLabel: GetStatusLabel(item),
                StatusPill: GetStatusPillClass(item),
                AlertThreshold: item.AlertThreshold,
                CriticalThreshold: item.CriticalThreshold
            );

            return new(true, $"Stok güncellendi. Yeni stok: {newStock}", result);
        }

        // =====================================================================
        //  PRIVATE HELPERS — Stok durum hesaplama
        //  (StockController'daki statik helper'larla birebir aynı mantık)
        // =====================================================================

        private static bool IsCritical(MenuItem m) =>
            m.TrackStock && m.CriticalThreshold > 0 && m.StockQuantity <= m.CriticalThreshold;

        private static bool IsLow(MenuItem m) =>
            m.TrackStock && m.AlertThreshold > 0 &&
            m.StockQuantity <= m.AlertThreshold && !IsCritical(m);

        private static string GetStatusString(MenuItem m)
        {
            if (!m.TrackStock) return "NotTracked";
            if (IsCritical(m)) return "Critical";
            if (IsLow(m)) return "Low";
            return "OK";
        }

        private static string GetStatusLabel(MenuItem m) => GetStatusString(m) switch
        {
            "Critical" => "🚨 Kritik",
            "Low" => "⚡ Düşük",
            "NotTracked" => "— Takip Dışı",
            _ => "✓ Yeterli"
        };

        private static string GetStatusPillClass(MenuItem m) => GetStatusString(m) switch
        {
            "Critical" => "pill-red",
            "Low" => "pill-amber",
            "NotTracked" => "pill-gray",
            _ => "pill-green"
        };
    }
}