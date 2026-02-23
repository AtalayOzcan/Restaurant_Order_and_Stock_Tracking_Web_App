using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly RestaurantDbContext _db;

        public OrdersController(RestaurantDbContext db)
        {
            _db = db;
        }
        // GET /Orders
        public async Task<IActionResult> Index(string tab = "active")
        {
            ViewData["Title"] = "Siparişler";
            ViewData["ActiveOrderCount"] = await _db.Orders.CountAsync(o => o.OrderStatus == "open");
            ViewData["ActiveTab"] = tab;

            var activeOrders = await _db.Orders
                .Where(o => o.OrderStatus == "open")
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderBy(o => o.OrderOpenedAt)
                .ToListAsync();

            var pastOrders = await _db.Orders
                .Where(o => o.OrderStatus == "paid" || o.OrderStatus == "cancelled")
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.OrderClosedAt)
                .Take(50) // son 50 sipariş
                .ToListAsync();

            ViewBag.ActiveOrders = activeOrders;
            ViewBag.PastOrders = pastOrders;

            return View();
        }

        // GET /Orders/Create/5  → adisyon açma sayfası
        public async Task<IActionResult> Create(int tableId)
        {
            var table = await _db.Tables.FindAsync(tableId);

            if (table == null)
            {
                TempData["Error"] = "Masa bulunamadı.";
                return RedirectToAction("Index", "Tables");
            }

            if (table.TableStatus == 1)
            {
                // Zaten açık adisyon var, direkt o adisyona git
                var existingOrder = await _db.Orders
                    .FirstOrDefaultAsync(o => o.TableId == tableId
                                           && o.OrderStatus == "open");
                if (existingOrder != null)
                    return RedirectToAction(nameof(Detail), new { id = existingOrder.OrderId });
            }

            // Menü kategorileriyle birlikte ürünleri çek
            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .Include(c => c.MenuItems.Where(m => m.IsAvailable))
                .ToListAsync();

            ViewData["Title"] = $"{table.TableName} — Adisyon Aç";
            ViewBag.Table = table;
            ViewBag.Categories = categories;

            return View();
        }

        // POST /Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int tableId, string openedBy, string? orderNote,
                                                List<int> menuItemIds, List<int> quantities,
                                                List<string?> itemNotes)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(openedBy))
            {
                TempData["Error"] = "Garson adı boş olamaz.";
                return RedirectToAction(nameof(Create), new { tableId });
            }

            if (menuItemIds == null || !menuItemIds.Any())
            {
                TempData["Error"] = "En az bir ürün eklemelisiniz.";
                return RedirectToAction(nameof(Create), new { tableId });
            }

            var table = await _db.Tables.FindAsync(tableId);
            if (table == null)
            {
                TempData["Error"] = "Masa bulunamadı.";
                return RedirectToAction("Index", "Tables");
            }

            // Transaction — tutarsız veri kalmasın
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Adisyon oluştur
                var order = new Order
                {
                    TableId = tableId,
                    OrderStatus = "open",
                    OrderOpenedBy = openedBy.Trim(),
                    OrderNote = orderNote?.Trim(),
                    OrderTotalAmount = 0,
                    OrderOpenedAt = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(); // OrderId üretilsin

                // 2) Sipariş kalemlerini ekle
                decimal total = 0;

                for (int i = 0; i < menuItemIds.Count; i++)
                {
                    var menuItem = await _db.MenuItems.FindAsync(menuItemIds[i]);
                    if (menuItem == null) continue;

                    int qty = quantities[i] < 1 ? 1 : quantities[i];
                    decimal lineTotal = menuItem.MenuItemPrice * qty;

                    var item = new OrderItem
                    {
                        OrderId = order.OrderId,
                        MenuItemId = menuItem.MenuItemId,
                        OrderItemQuantity = qty,
                        OrderItemUnitPrice = menuItem.MenuItemPrice,
                        OrderItemLineTotal = lineTotal,
                        OrderItemNote = itemNotes.ElementAtOrDefault(i)?.Trim(),
                        OrderItemStatus = "pending",
                        OrderItemAddedAt = DateTime.UtcNow
                    };

                    _db.OrderItems.Add(item);
                    total += lineTotal;

                    // 3) Stok takibi açıksa stok düş
                    if (menuItem.TrackStock)
                    {
                        menuItem.StockQuantity -= qty;
                        if (menuItem.StockQuantity <= 0)
                        {
                            menuItem.StockQuantity = 0;
                            menuItem.IsAvailable = false;
                        }
                    }
                }

                // 4) Toplam güncelle
                order.OrderTotalAmount = total;

                // 5) Masayı dolu yap
                table.TableStatus = 1;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Adisyon açıldı.";
                return RedirectToAction(nameof(Detail), new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Adisyon açılırken hata oluştu. Tekrar deneyin.";
                return RedirectToAction(nameof(Create), new { tableId });
            }
        }

        // GET /Orders/Detail/42
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems.OrderBy(i => i.OrderItemAddedAt))
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Adisyon bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Ödeme için menü kategorileri (yeni ürün eklemek için)
            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .Include(c => c.MenuItems.Where(m => m.IsAvailable))
                .ToListAsync();

            ViewData["Title"] = $"{order.Table?.TableName} — Adisyon #{order.OrderId}";
            ViewBag.Categories = categories;

            return View(order);
        }

        // POST /Orders/UpdateItemStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemStatus(int orderItemId, string newStatus, int orderId)
        {
            var validStatuses = new[] { "pending", "preparing", "served", "cancelled" };
            if (!validStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Geçersiz durum.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            var item = await _db.OrderItems.FindAsync(orderItemId);
            if (item == null)
            {
                TempData["Error"] = "Kalem bulunamadı.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            item.OrderItemStatus = newStatus;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // POST /Orders/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int orderId, int menuItemId, int quantity, string? note)
        {
            var order = await _db.Orders.FindAsync(orderId);
            var menuItem = await _db.MenuItems.FindAsync(menuItemId);

            if (order == null || menuItem == null)
            {
                TempData["Error"] = "Adisyon veya ürün bulunamadı.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            if (order.OrderStatus != "open")
            {
                TempData["Error"] = "Kapalı adisyona ürün eklenemez.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            if (quantity < 1) quantity = 1;

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var item = new OrderItem
                {
                    OrderId = orderId,
                    MenuItemId = menuItemId,
                    OrderItemQuantity = quantity,
                    OrderItemUnitPrice = menuItem.MenuItemPrice,
                    OrderItemLineTotal = menuItem.MenuItemPrice * quantity,
                    OrderItemNote = note?.Trim(),
                    OrderItemStatus = "pending",
                    OrderItemAddedAt = DateTime.UtcNow
                };

                _db.OrderItems.Add(item);

                // Toplam güncelle
                order.OrderTotalAmount += item.OrderItemLineTotal;

                // Stok düş
                if (menuItem.TrackStock)
                {
                    menuItem.StockQuantity -= quantity;
                    if (menuItem.StockQuantity <= 0)
                    {
                        menuItem.StockQuantity = 0;
                        menuItem.IsAvailable = false;
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"{menuItem.MenuItemName} eklendi.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Ürün eklenirken hata oluştu.";
            }

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // POST /Orders/AddPayment  — kısmi ödeme ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(int orderId, string payerName,
            string paymentMethod, decimal paymentAmount, decimal discountAmount)
        {
            var order = await _db.Orders
                .Include(o => o.Payments)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Adisyon bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (order.OrderStatus != "open")
            {
                TempData["Error"] = "Bu adisyon zaten kapatılmış.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            // Validasyon
            if (paymentAmount <= 0)
            {
                TempData["Error"] = "Ödeme tutarı 0'dan büyük olmalıdır.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            if (discountAmount < 0)
            {
                TempData["Error"] = "İndirim tutarı negatif olamaz.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            // İndirim sonrası net tutar
            var netTotal = order.OrderTotalAmount - discountAmount;
            var alreadyPaid = order.Payments.Sum(p => p.PaymentsAmount);
            var remaining = netTotal - alreadyPaid;

            if (paymentAmount > remaining + 0.01m) // küçük float toleransı
            {
                TempData["Error"] = $"Ödeme tutarı kalan tutarı (₺{remaining:N2}) aşamaz.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            int methodCode = paymentMethod switch
            {
                "credit_card" => 1,
                "debit_card" => 2,
                "other" => 3,
                _ => 0   // nakit
            };

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var payment = new Payment
                {
                    OrderId = orderId,
                    PaymentsMethod = methodCode,
                    PaymentsAmount = paymentAmount,
                    PaymentsChangeGiven = 0,
                    PaymentsPaidAt = DateTime.UtcNow,
                    PaymentsNote = string.IsNullOrWhiteSpace(payerName)
                                            ? "" : payerName.Trim()
                };

                _db.Payments.Add(payment);

                // Tüm tutar ödendi mi? → adisyonu kapat
                var newTotalPaid = alreadyPaid + paymentAmount;
                if (newTotalPaid >= netTotal - 0.01m)
                {
                    order.OrderStatus = "paid";
                    order.OrderClosedAt = DateTime.UtcNow;

                    if (order.Table != null)
                        order.Table.TableStatus = 0; // masayı boşalt
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (order.OrderStatus == "paid")
                {
                    TempData["Success"] = "Adisyon kapatıldı, ödeme tamamlandı.";
                    return RedirectToAction("Index", "Tables");
                }

                TempData["Success"] = $"₺{paymentAmount:N2} ödeme alındı. Kalan: ₺{netTotal - newTotalPaid:N2}";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Ödeme kaydedilirken hata oluştu.";
            }

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // POST /Orders/Close
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int orderId, string paymentMethod, decimal paymentAmount)
        {
            var order = await _db.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Adisyon bulunamadı.";
                return RedirectToAction("Index", "Tables");
            }

            if (order.OrderStatus != "open")
            {
                TempData["Error"] = "Bu adisyon zaten kapatılmış.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            if (paymentAmount < order.OrderTotalAmount)
            {
                TempData["Error"] = "Ödeme tutarı toplam tutardan az olamaz.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Ödeme kaydı
                var payment = new Payment
                {
                    OrderId = orderId,
                    PaymentsMethod = paymentMethod == "card" ? 1 : 0,
                    PaymentsAmount = paymentAmount,
                    PaymentsChangeGiven = paymentAmount - order.OrderTotalAmount,
                    PaymentsPaidAt = DateTime.UtcNow,
                    PaymentsNote = ""
                };

                _db.Payments.Add(payment);

                // 2) Adisyonu kapat
                order.OrderStatus = "paid";
                order.OrderClosedAt = DateTime.UtcNow;

                // 3) Masayı boşalt
                order.Table.TableStatus = 0;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Adisyon kapatıldı, ödeme alındı.";
                return RedirectToAction("Index", "Tables");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Adisyon kapatılırken hata oluştu.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }
        }
    }
}