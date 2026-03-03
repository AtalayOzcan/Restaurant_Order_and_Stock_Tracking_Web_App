// ════════════════════════════════════════════════════════════════════════════
//  Controllers/HomeController.cs
//  Yol: Restaurant_Order_and_Stock_Tracking_Web_App.MVC/Controllers/
// ════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Dashboard;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly RestaurantDbContext _db;

        public HomeController(RestaurantDbContext db)
        {
            _db = db;
        }

        // ── GET: /Home/Index (Dashboard) ─────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";

            // ── Tarih aralıkları ──────────────────────────────────────────────
            // PostgreSQL UTC kullandığı için DateTime.UtcNow; SQL Server kullananlar
            // DateTime.Today kullanabilir. Projenizin timezone ayarına göre düzeltin.
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);
            var yesterdayStart = todayStart.AddDays(-1);
            var yesterdayEnd = todayStart;

            // ── 1. BUGÜNÜN SİPARİŞ VERİLERİNİ ÇEK ───────────────────────────
            // Hem "open" hem "paid" siparişler (bugün açılanlar)
            var todayOrders = await _db.Orders
                .Where(o => o.OrderOpenedAt >= todayStart && o.OrderOpenedAt < todayEnd)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderStatus,
                    o.OrderTotalAmount,
                    o.OrderOpenedAt,
                    o.OrderClosedAt
                })
                .ToListAsync();

            // Sadece ödenmiş siparişler → ciro hesabı
            var paidToday = todayOrders.Where(o => o.OrderStatus == "paid").ToList();

            decimal dailyRevenue = paidToday.Sum(o => o.OrderTotalAmount);
            int totalOrders = todayOrders.Count;
            decimal avgOrderValue = paidToday.Count > 0
                ? Math.Round(dailyRevenue / paidToday.Count, 2)
                : 0;
            int activeOrdersNow = todayOrders.Count(o => o.OrderStatus == "open");

            // ── 2. DÜN CIRO (Trend hesabı) ───────────────────────────────────
            decimal yesterdayRevenue = await _db.Orders
                .Where(o => o.OrderStatus == "paid"
                         && o.OrderClosedAt >= yesterdayStart
                         && o.OrderClosedAt < yesterdayEnd)
                .SumAsync(o => o.OrderTotalAmount);

            decimal? trendPct = null;
            if (yesterdayRevenue > 0)
            {
                trendPct = Math.Round((dailyRevenue - yesterdayRevenue) / yesterdayRevenue * 100, 1);
            }
            else if (dailyRevenue > 0)
            {
                trendPct = 100m; // Dün 0, bugün pozitif → %100 artış göster
            }

            // ── 3. MASA DURUMU ────────────────────────────────────────────────
            var tableStatusRaw = await _db.Tables
                .GroupBy(t => t.TableStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var tableStatus = new TableStatusSummaryData
            {
                Empty = tableStatusRaw.FirstOrDefault(x => x.Status == 0)?.Count ?? 0,
                Occupied = tableStatusRaw.FirstOrDefault(x => x.Status == 1)?.Count ?? 0,
                Reserved = tableStatusRaw.FirstOrDefault(x => x.Status == 2)?.Count ?? 0
            };

            // ── 4. SAATLİK YOĞUNLUK ───────────────────────────────────────────
            // Restoran için anlamlı saat aralığı: 08:00 - 23:00
            var hourlyTrends = new List<HourlyTrendPoint>();
            var currentHour = DateTime.UtcNow.Hour;

            for (int h = 8; h <= 23; h++)
            {
                var slotStart = todayStart.AddHours(h);
                var slotEnd = slotStart.AddHours(1);

                // Henüz gelmemiş saatler için 0 göster ama grafiği bozmamak için dahil et
                var ordersInSlot = todayOrders
                    .Where(o => o.OrderOpenedAt >= slotStart && o.OrderOpenedAt < slotEnd)
                    .ToList();

                decimal slotRevenue = ordersInSlot
                    .Where(o => o.OrderStatus == "paid")
                    .Sum(o => o.OrderTotalAmount);

                hourlyTrends.Add(new HourlyTrendPoint
                {
                    HourLabel = $"{h:D2}:00",
                    OrderCount = ordersInSlot.Count,
                    Revenue = slotRevenue
                });
            }

            // ── 5. EN ÇOK SATAN 5 ÜRÜN ───────────────────────────────────────
            // Bugün açılan siparişlerin order item'larından hesapla
            var topProducts = await _db.OrderItems
                .Where(oi => oi.Order.OrderOpenedAt >= todayStart
                          && oi.Order.OrderOpenedAt < todayEnd
                          && oi.CancelledQuantity < oi.OrderItemQuantity) // iptal edilmemişler
                .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem.MenuItemName })
                .Select(g => new TopProductPoint
                {
                    ProductName = g.Key.MenuItemName,
                    Quantity = g.Sum(x => x.OrderItemQuantity - x.CancelledQuantity),
                    Revenue = g.Sum(x => x.OrderItemLineTotal)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

            // ── 6. GARSON ÇAĞRILARI ────────────────────────────────────────────
            int waiterCalls = await _db.Tables.CountAsync(t => t.IsWaiterCalled);

            // ── 7. DÜŞÜK STOK UYARILARI ───────────────────────────────────────
            int lowStock = await _db.MenuItems.CountAsync(
                m => !m.IsDeleted
                  && m.TrackStock
                  && m.StockQuantity <= m.AlertThreshold
                  && m.AlertThreshold > 0);

            // ── ViewModel Oluştur ─────────────────────────────────────────────
            var vm = new DashboardViewModel
            {
                DailyTotalRevenue = dailyRevenue,
                RevenueTrendPercentage = trendPct,
                TotalOrdersToday = totalOrders,
                AverageOrderValue = avgOrderValue,
                ActiveOrdersNow = activeOrdersNow,
                TableStatus = tableStatus,
                HourlyOrderTrends = hourlyTrends,
                TopSellingProducts = topProducts,
                WaiterCallsActive = waiterCalls,
                LowStockAlerts = lowStock
            };

            return View(vm);
        }
    }
}