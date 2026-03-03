using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class QrMenuController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly IHubContext<RestaurantHub> _hub;

        private static readonly HashSet<string> _validLangs =
            new(StringComparer.OrdinalIgnoreCase) { "tr", "en", "ar", "ru" };

        public QrMenuController(RestaurantDbContext context, IHubContext<RestaurantHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // ── GET /QrMenu/Index/{tableName} ──────────────────────────────
        [HttpGet]
        [Route("QrMenu/Index/{tableName}")]
        public async Task<IActionResult> Index(string tableName)
        {
            var decodedName = Uri.UnescapeDataString(tableName);

            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.TableName == decodedName);

            if (table == null)
                return NotFound("Bu QR koda ait masa bulunamadı.");

            // ── Dil seçimi: önce query string, sonra cookie, varsayılan TR ──
            string lang = "tr";

            if (!string.IsNullOrWhiteSpace(Request.Query["lang"]))
            {
                var qLang = Request.Query["lang"].ToString().ToLower();
                if (_validLangs.Contains(qLang)) lang = qLang;
            }
            else if (Request.Cookies.TryGetValue("qrmenu_lang", out var cookieLang)
                     && _validLangs.Contains(cookieLang?.ToLower() ?? ""))
            {
                lang = cookieLang!.ToLower();
            }

            // Cookie'yi tazeliyoruz (30 gün)
            Response.Cookies.Append("qrmenu_lang", lang, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false,    // JS okuyabilsin
                SameSite = SameSiteMode.Lax
            });

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

            categories = categories
                .Where(c => c.MenuItems != null && c.MenuItems.Any())
                .ToList();

            ViewData["Title"] = $"{table.TableName} — Menü";
            ViewData["TableName"] = table.TableName;
            ViewData["IsWaiterCalled"] = table.IsWaiterCalled;
            ViewData["Lang"] = lang;   // ← View bunu okur

            return View(categories);
        }

        // ── POST /QrMenu/CallWaiter ────────────────────────────────────
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

            if (table.IsWaiterCalled)
                return Ok(new { success = true, alreadyCalled = true, message = "Garson zaten çağrıldı." });

            table.IsWaiterCalled = true;
            table.WaiterCalledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("WaiterCalled", new
            {
                tableName = table.TableName,
                calledAtUtc = table.WaiterCalledAt!.Value.ToString("o")
            });

            return Ok(new { success = true, alreadyCalled = false, message = "Garson çağrıldı." });
        }
    }

    public class CallWaiterRequest
    {
        public string TableName { get; set; } = string.Empty;
    }
}