// ============================================================================
//  Areas/App/Controllers/StockController.cs
//
//  REFACTORING: UpdateStock() 80 satır if/else → IStockService (8 satır)
//
//  ESKİ DURUM (kaldırıldı):
//    UpdateStock() → 80 satır iş mantığı doğrudan action içinde
//    → direct / fire / movement modları controller'da
//    → StockLog yazımı controller'da
//    → Test edilemez
//
//  YENİ DURUM:
//    UpdateStock() → IStockService.UpdateStockAsync() → 8 satır
//    Diğer tüm action'lar (Index, GetHistory, ToggleTrack, GenerateStockPdfReport)
//    değiştirilmedi — sadece UpdateStock refactor edildi.
//
//  PROGRAM.CS'E EKLENMESİ GEREKEN:
//    builder.Services.AddScoped<IStockService, StockService>();
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Stock;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Shared.Common;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Areas.App.Controllers
{
    [Area("App")]
    [Authorize(Roles = "Admin")]
    public class StockController : AppBaseController
    {
        private readonly RestaurantDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IStockService _stockService;

        public StockController(
            RestaurantDbContext context,
            ITenantService tenantService,
            IStockService stockService)
        {
            _context = context;
            _tenantService = tenantService;
            _stockService = stockService;
        }

        // ── GET: /App/Stock ──────────────────────────────────────────────────
        public async Task<IActionResult> Index(bool showAll = false)
        {
            ViewData["Title"] = "Stok Yönetimi";
            ViewData["ShowAll"] = showAll;

            var allItems = await _context.MenuItems
                .Where(m => !m.IsDeleted)
                .Include(m => m.Category)
                .OrderBy(m => m.Category.CategorySortOrder)
                .ThenBy(m => m.MenuItemName)
                .ToListAsync();

            var displayItems = showAll
                ? allItems
                : allItems.Where(m => m.TrackStock).ToList();

            int totalProducts = allItems.Count;
            int trackedProducts = allItems.Count(m => m.TrackStock);
            int lowStockCount = allItems.Count(m => IsLow(m));
            int criticalCount = allItems.Count(m => IsCritical(m));

            ViewData["TotalProducts"] = totalProducts;
            ViewData["TrackedProducts"] = trackedProducts;
            ViewData["LowStockCount"] = lowStockCount;
            ViewData["CriticalCount"] = criticalCount;

            bool hasAlert = allItems.Any(m => IsLow(m) || IsCritical(m));
            ViewData["HasLowStock"] = hasAlert;
            ViewData["HasAlert"] = hasAlert;

            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            var allIds = allItems.Select(m => m.MenuItemId).ToList();

            var recentLogs = await _context.StockLogs
                .Where(l => allIds.Contains(l.MenuItemId))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var sparklineMap = allIds.ToDictionary(
                id => id,
                id => recentLogs
                    .Where(l => l.MenuItemId == id)
                    .Take(5).Select(l => l.NewStock).Reverse().ToList()
            );
            ViewData["SparklineMap"] = sparklineMap;

            var lastUpdatedMap = allIds.ToDictionary(
                id => id,
                id =>
                {
                    var last = recentLogs.FirstOrDefault(l => l.MenuItemId == id);
                    return last?.CreatedAt ?? allItems.First(m => m.MenuItemId == id).MenuItemCreatedTime;
                }
            );
            ViewData["LastUpdatedMap"] = lastUpdatedMap;

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var consumed = await _context.OrderItems
                .Where(oi =>
                    allIds.Contains(oi.MenuItemId) &&
                    oi.OrderItemAddedAt >= thirtyDaysAgo &&
                    oi.OrderItemStatus != OrderItemStatus.Cancelled)
                .GroupBy(oi => oi.MenuItemId)
                .Select(g => new
                {
                    MenuItemId = g.Key,
                    Consumed = g.Sum(oi => oi.OrderItemQuantity - oi.CancelledQuantity)
                })
                .ToDictionaryAsync(g => g.MenuItemId, g => g.Consumed);

            ViewData["ConsumedMap"] = consumed;

            return View(displayItems);
        }

        // ── POST: /App/Stock/UpdateStock ─────────────────────────────────────
        // REFACTORED: 80 satır if/else → IStockService'e taşındı (8 satır)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock([FromBody] StockUpdateDto dto)
        {
            var tenantId = _tenantService.TenantId ?? string.Empty;
            var result = await _stockService.UpdateStockAsync(dto, tenantId);

            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            var d = result.Data!;
            return Json(new
            {
                success = true,
                newStock = d.NewStock,
                status = d.Status,
                statusLabel = d.StatusLabel,
                statusPill = d.StatusPill,
                alertThreshold = d.AlertThreshold,
                criticalThreshold = d.CriticalThreshold,
                message = result.Message
            });
        }

        // ── GET: /App/Stock/GetHistory/{id} ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            if (item.TenantId != _tenantService.TenantId)
                return Json(new { success = false, message = "Bu ürüne erişim yetkiniz yok." });

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
                    note = l.Note ?? "—",
                    sourceType = l.SourceType ?? "",
                    orderId = l.OrderId
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

        // ── POST: /App/Stock/ToggleTrack ─────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrack([FromBody] StockToggleTrackDto dto)
        {
            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == dto.MenuItemId);

            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            item.TrackStock = dto.TrackStock;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                trackStock = item.TrackStock,
                status = GetStatusString(item),
                statusLabel = GetStatusLabel(item),
                statusPill = GetStatusPillClass(item),
                message = item.TrackStock
                    ? "Stok takibi aktif edildi."
                    : "Stok takibi kapatıldı."
            });
        }

        // ── GET: /App/Stock/GenerateStockPdfReport ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> GenerateStockPdfReport(
            string? search = null,
            string? category = null,
            string? status = null,
            bool showAll = false)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var query = _context.MenuItems
                .Where(m => !m.IsDeleted)
                .Include(m => m.Category)
                .AsQueryable();

            if (!showAll)
                query = query.Where(m => m.TrackStock);

            var allItems = await query
                .OrderBy(m => m.Category.CategorySortOrder)
                .ThenBy(m => m.MenuItemName)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var sq = search.ToLower();
                allItems = allItems.Where(m =>
                    m.MenuItemName.ToLower().Contains(sq) ||
                    $"SKU-{m.MenuItemId:D4}".ToLower().Contains(sq)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(category))
                allItems = allItems.Where(m => m.Category.CategoryName == category).ToList();

            if (!string.IsNullOrWhiteSpace(status))
            {
                allItems = status switch
                {
                    "Critical" => allItems.Where(m => IsCritical(m)).ToList(),
                    "Low" => allItems.Where(m => IsLow(m)).ToList(),
                    "OK" => allItems.Where(m => m.TrackStock && !IsLow(m) && !IsCritical(m)).ToList(),
                    "NotTracked" => allItems.Where(m => !m.TrackStock).ToList(),
                    _ => allItems
                };
            }

            var reportDate = DateTime.Now;
            var restaurantName = "Restoran Stok Raporu";
            var headerBg = "#1e293b";
            var rowEven = "#f8fafc";
            var rowOdd = "#ffffff";
            var criticalClr = "#ef4444";
            var lowClr = "#f59e0b";
            var okClr = "#22c55e";
            var grayClr = "#94a3b8";
            var borderClr = "#e2e8f0";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(restaurantName)
                                    .FontSize(18).Bold().FontColor("#1e293b");
                                c.Item().Text($"Stok Durum Raporu — {reportDate:dd MMMM yyyy, HH:mm}")
                                    .FontSize(9).FontColor("#64748b");
                            });
                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Toplam: {allItems.Count} ürün")
                                    .FontSize(9).FontColor("#64748b").AlignRight();
                                c.Item().Text($"Kritik: {allItems.Count(IsCritical)}  |  Düşük: {allItems.Count(IsLow)}")
                                    .FontSize(9).FontColor("#64748b").AlignRight();
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#1e293b");
                        col.Item().PaddingBottom(6);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Sayfa ").FontSize(8).FontColor("#94a3b8");
                        text.CurrentPageNumber().FontSize(8).FontColor("#94a3b8");
                        text.Span(" / ").FontSize(8).FontColor("#94a3b8");
                        text.TotalPages().FontSize(8).FontColor("#94a3b8");
                        text.Span($"   •   Oluşturulma: {reportDate:dd.MM.yyyy HH:mm}")
                            .FontSize(8).FontColor("#94a3b8");
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3.5f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2f);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#1e293b").Padding(7).AlignMiddle();

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell)
                                .Text("Ürün Adı").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Element(HeaderCell)
                                .Text("SKU").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Element(HeaderCell)
                                .Text("Kategori").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Element(HeaderCell)
                                .Text("Güncel Stok").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Element(HeaderCell)
                                .Text("Uyarı Eşiği").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Element(HeaderCell)
                                .Text("Kritik Eşiği").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Element(HeaderCell)
                                .Text("Stok Durumu").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        for (int i = 0; i < allItems.Count; i++)
                        {
                            var item = allItems[i];
                            var bg = i % 2 == 0 ? rowEven : rowOdd;
                            var statusStr = GetStatusString(item);
                            var (statusLabel, statusColor) = statusStr switch
                            {
                                "Critical" => ("🚨 Kritik", criticalClr),
                                "Low" => ("⚡ Düşük", lowClr),
                                "NotTracked" => ("— Takip Dışı", grayClr),
                                _ => ("✓ Yeterli", okClr)
                            };

                            IContainer DataCell(IContainer c) =>
                                c.Background(bg).BorderBottom(0.5f).BorderColor(borderClr)
                                 .Padding(6).AlignMiddle();

                            table.Cell().Element(DataCell)
                                .Text(item.MenuItemName).FontSize(9);
                            table.Cell().Element(DataCell)
                                .Text($"SKU-{item.MenuItemId:D4}").FontSize(8).FontColor("#64748b");
                            table.Cell().Element(DataCell)
                                .Text(item.Category?.CategoryName ?? "—").FontSize(9);
                            table.Cell().Element(DataCell)
                                .Text(item.StockQuantity.ToString())
                                .FontSize(9).Bold().AlignCenter();
                            table.Cell().Element(DataCell)
                                .Text(item.AlertThreshold > 0 ? item.AlertThreshold.ToString() : "—")
                                .FontSize(9).FontColor("#64748b").AlignCenter();
                            table.Cell().Element(DataCell)
                                .Text(item.CriticalThreshold > 0 ? item.CriticalThreshold.ToString() : "—")
                                .FontSize(9).FontColor("#64748b").AlignCenter();
                            table.Cell().Element(DataCell)
                                .Text(statusLabel).FontSize(9).Bold().FontColor(statusColor).AlignCenter();
                        }
                    });
                });
            }).GeneratePdf();

            var fileName = $"stok_raporu_{reportDate:yyyyMMdd_HHmm}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // ── Private helpers — Stok durum hesaplama ───────────────────────────
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