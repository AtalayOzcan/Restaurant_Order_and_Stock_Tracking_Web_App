using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    /// <summary>
    /// Müşterilerin QR kod ile açtığı Fine-Dining menü ekranı.
    /// Kimlik doğrulama gerektirmez — [AllowAnonymous] davranışı varsayılandır
    /// (sınıf düzeyinde [Authorize] yoktur).
    /// URL örneği: /QrMenu/Index/Masa-1
    ///             /QrMenu/Index/Teras%201   (boşluk URL-encoded)
    /// </summary>
    public class QrMenuController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly IHubContext<RestaurantHub> _hub;

        public QrMenuController(RestaurantDbContext context, IHubContext<RestaurantHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // ── GET /QrMenu/Index/{tableName} ──────────────────────────────────────
        [HttpGet]
        [Route("QrMenu/Index/{tableName}")]
        public async Task<IActionResult> Index(string tableName)
        {
            // URL-encode karakterleri çöz (ör. "Teras%201" → "Teras 1")
            var decodedName = Uri.UnescapeDataString(tableName);

            // Masa doğrulama
            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.TableName == decodedName);

            if (table == null)
                return NotFound("Bu QR koda ait masa bulunamadı.");

            // ── Dinamik menü verisi ───────────────────────────────────────────
            // Kategoriler: CategorySortOrder'a göre sıralı, yalnızca aktif olanlar.
            // Her kategorinin altındaki ürünler:
            //   - IsDeleted = false
            //   - IsAvailable = true  VEYA  (TrackStock = true ve StockQuantity > 0)
            //   - Ekleniş zamanına göre sıralı (MenuItemCreatedTime ASC)
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
                .Include(c => c.MenuItems
                    .Where(m =>
                        !m.IsDeleted &&
                        (m.IsAvailable || (m.TrackStock && m.StockQuantity > 0))
                    )
                    .OrderBy(m => m.MenuItemCreatedTime)
                )
                .ToListAsync();

            // Ürün içermeyen kategorileri filtrele (arayüzde boş blok gözükmesin)
            categories = categories
                .Where(c => c.MenuItems != null && c.MenuItems.Any())
                .ToList();

            ViewData["Title"] = $"{table.TableName} — Menü";
            ViewData["TableName"] = table.TableName;
            ViewData["IsWaiterCalled"] = table.IsWaiterCalled;

            return View(categories);
        }

        // ── POST /QrMenu/CallWaiter ────────────────────────────────────────────
        /// <summary>
        /// Müşteri "Garson Çağır" butonuna basınca çağrılır.
        /// Payload: { "TableName": "Masa 1" }
        /// SignalR ile tüm bağlı admin/garson ekranlarına anlık bildirim gönderir.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("QrMenu/CallWaiter")]
        public async Task<IActionResult> CallWaiter([FromBody] CallWaiterRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.TableName))
                return BadRequest(new { success = false, message = "Geçersiz masa adı." });

            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.TableName == request.TableName);

            if (table == null)
                return NotFound(new { success = false, message = "Masa bulunamadı." });

            // Zaten çağrıldıysa tekrar işlem yapma — sadece onayla
            if (table.IsWaiterCalled)
                return Ok(new { success = true, alreadyCalled = true, message = "Garson zaten çağrıldı." });

            table.IsWaiterCalled = true;
            await _context.SaveChangesAsync();

            // SignalR: Tüm bağlı garson / admin ekranlarına anlık bildir
            await _hub.Clients.All.SendAsync("WaiterCalled", new
            {
                tableName = table.TableName
            });

            return Ok(new { success = true, alreadyCalled = false, message = "Garson çağrıldı." });
        }
    }

    /// <summary>Müşteri tarafından gönderilen CallWaiter istek gövdesi.</summary>
    public class CallWaiterRequest
    {
        public string TableName { get; set; } = string.Empty;
    }
}