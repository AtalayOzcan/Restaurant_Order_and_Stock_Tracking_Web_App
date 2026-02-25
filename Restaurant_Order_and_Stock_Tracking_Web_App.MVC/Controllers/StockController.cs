using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class StockController : Controller
    {
        private readonly RestaurantDbContext _context;

        // Eşiğin bu oranının altına düşünce "Kritik" sayılır (örn. 0.5 = %50 altı)
        private const double CriticalRatio = 0.5;

        public StockController(RestaurantDbContext context)
        {
            _context = context;
        }

        // ── GET: /Stock ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Stok Yönetimi";

            var menuItems = await _context.MenuItems
                .Where(m => !m.IsDeleted)
                .Include(m => m.Category)
                .OrderBy(m => m.Category.CategorySortOrder)
                .ThenBy(m => m.MenuItemName)
                .ToListAsync();

            // ── Özet istatistikler ───────────────────────────────────
            ViewData["TotalProducts"] = menuItems.Count;
            ViewData["TrackedProducts"] = menuItems.Count(m => m.TrackStock);
            ViewData["LowStockCount"] = menuItems.Count(m => IsLow(m));
            ViewData["CriticalCount"] = menuItems.Count(m => IsCritical(m));

            bool hasAlert = menuItems.Any(m => IsLow(m) || IsCritical(m));
            ViewData["HasLowStock"] = hasAlert;   // Layout topbar bildirimi için
            ViewData["HasAlert"] = hasAlert;   // Sayfa banner için

            // ── Kategori filtre dropdown ─────────────────────────────
            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            // ── Her ürün için son 5 log → sparkline verileri ─────────
            // Dictionary<menuItemId, List<int>> olarak ViewData'ya gönderilir
            var allIds = menuItems.Select(m => m.MenuItemId).ToList();

            var recentLogs = await _context.StockLogs
                .Where(l => allIds.Contains(l.MenuItemId))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var sparklineMap = allIds.ToDictionary(
                id => id,
                id => recentLogs
                    .Where(l => l.MenuItemId == id)
                    .Take(5)
                    .Select(l => l.NewStock)
                    .Reverse()
                    .ToList()
            );
            ViewData["SparklineMap"] = sparklineMap;

            // ── Son güncelleme tarihleri ─────────────────────────────
            var lastUpdatedMap = allIds.ToDictionary(
                id => id,
                id =>
                {
                    var last = recentLogs.FirstOrDefault(l => l.MenuItemId == id);
                    return last?.CreatedAt
                        ?? menuItems.First(m => m.MenuItemId == id).MenuItemCreatedTime;
                }
            );
            ViewData["LastUpdatedMap"] = lastUpdatedMap;

            return View(menuItems);
        }

        // ── POST: /Stock/UpdateStock  (AJAX JSON) ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(
            int menuItemId,
            string updateMode,          // "direct" | "movement"
            int? newStockValue,         // direct mod
            string? movementDirection,  // "in" | "out"  — movement mod
            int? movementQuantity,      // movement mod miktarı
            string? note,               // movement mod notu (zorunlu) | direct mod (opsiyonel)
            int? alertThreshold)        // opsiyonel, her iki modda da güncellenebilir
        {
            var item = await _context.MenuItems.FindAsync(menuItemId);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            int previousStock = item.StockQuantity;
            int newStock;
            string movementType;
            int quantityChange;

            if (updateMode == "direct")
            {
                // ── Direkt Giriş (Sayım Düzeltmesi) ─────────────────
                if (newStockValue == null || newStockValue < 0)
                    return Json(new { success = false, message = "Geçerli bir stok değeri giriniz." });

                newStock = newStockValue.Value;
                quantityChange = newStock - previousStock;
                movementType = "Düzeltme";
            }
            else
            {
                // ── Hareket Bazlı (Giriş / Çıkış) ───────────────────
                if (movementQuantity == null || movementQuantity <= 0)
                    return Json(new { success = false, message = "Geçerli bir miktar giriniz." });

                if (string.IsNullOrWhiteSpace(note))
                    return Json(new { success = false, message = "Hareket bazlı işlem için açıklama zorunludur." });

                if (movementDirection == "in")
                {
                    quantityChange = movementQuantity.Value;
                    movementType = "Giriş";
                }
                else
                {
                    quantityChange = -movementQuantity.Value;
                    movementType = "Çıkış";
                }

                newStock = previousStock + quantityChange;
                if (newStock < 0)
                    return Json(new { success = false, message = "Stok miktarı sıfırın altına düşemez." });
            }

            // ── AlertThreshold güncelle (opsiyonel) ──────────────────
            if (alertThreshold.HasValue && alertThreshold.Value >= 0)
                item.AlertThreshold = alertThreshold.Value;

            // ── Stoğu güncelle ───────────────────────────────────────
            item.StockQuantity = newStock;

            // ── StockLog kaydı oluştur ────────────────────────────────
            _context.StockLogs.Add(new StockLog
            {
                MenuItemId = item.MenuItemId,
                MovementType = movementType,
                QuantityChange = quantityChange,
                PreviousStock = previousStock,
                NewStock = newStock,
                Note = note?.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newStock = newStock,
                status = GetStatusString(item),
                statusLabel = GetStatusLabel(item),
                statusPill = GetStatusPillClass(item),
                message = $"Stok güncellendi. Yeni stok: {newStock}"
            });
        }

        // ── GET: /Stock/GetHistory/5  (AJAX JSON) ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            var logs = await _context.StockLogs
                .Where(l => l.MenuItemId == id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .Select(l => new
                {
                    l.StockLogId,
                    createdAt = l.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    l.MovementType,
                    l.QuantityChange,
                    l.PreviousStock,
                    l.NewStock,
                    note = l.Note ?? "—"
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                itemName = item.MenuItemName,
                sku = $"SKU-{item.MenuItemId:D4}",
                logs
            });
        }

        // ── POST: /Stock/ToggleTrack  (AJAX JSON) ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrack(int menuItemId, bool trackStock)
        {
            var item = await _context.MenuItems.FindAsync(menuItemId);
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            item.TrackStock = trackStock;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                trackStock = item.TrackStock,
                status = GetStatusString(item),
                statusLabel = GetStatusLabel(item),
                statusPill = GetStatusPillClass(item),
                message = item.TrackStock ? "Stok takibi aktif edildi." : "Stok takibi kapatıldı."
            });
        }

        // ── Private helpers ──────────────────────────────────────────

        private static bool IsCritical(MenuItem m) =>
            m.TrackStock && m.AlertThreshold > 0 &&
            m.StockQuantity <= (int)(m.AlertThreshold * CriticalRatio);

        private static bool IsLow(MenuItem m) =>
            m.TrackStock && m.AlertThreshold > 0 &&
            m.StockQuantity <= m.AlertThreshold &&
            !IsCritical(m);

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