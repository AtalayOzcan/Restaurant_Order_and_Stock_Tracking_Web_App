using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class TablesController : Controller
    {
        private readonly RestaurantDbContext _db;

        public TablesController(RestaurantDbContext db)
        {
            _db = db;
        }

        // GET /Tables
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Masalar";
            ViewData["OccupiedCount"] = await _db.Tables
                                            .CountAsync(t => t.TableStatus == 1);

            var tables = await _db.Tables
                                  .OrderBy(t => t.TableName)
                                  .ToListAsync();

            return View(tables);
        }

        // POST /Tables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tableName, int tableCapacity)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(tableName))
            {
                TempData["Error"] = "Masa adı boş olamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (tableCapacity < 1 || tableCapacity > 20)
            {
                TempData["Error"] = "Kapasite 1 ile 20 arasında olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            // Aynı isimde masa var mı?
            var exists = await _db.Tables
                                  .AnyAsync(t => t.TableName == tableName.Trim());
            if (exists)
            {
                TempData["Error"] = $"'{tableName}' adında bir masa zaten var.";
                return RedirectToAction(nameof(Index));
            }

            var table = new Table
            {
                TableName = tableName.Trim(),
                TableCapacity = tableCapacity,
                TableStatus = 0,
                TableCreatedAt = DateTime.UtcNow
            };

            _db.Tables.Add(table);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"'{table.TableName}' başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Tables/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int tableId, string reservationName,
            string reservationPhone, int reservationGuestCount, string reservationTime)
        {
            var table = await _db.Tables.FindAsync(tableId);

            if (table == null)
            {
                TempData["Error"] = "Masa bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (table.TableStatus != 0)
            {
                TempData["Error"] = "Yalnızca boş masalar rezerve edilebilir.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reservationName))
            {
                TempData["Error"] = "İsim soyisim boş olamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reservationPhone))
            {
                TempData["Error"] = "Telefon numarası boş olamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (reservationGuestCount < 1 || reservationGuestCount > table.TableCapacity)
            {
                TempData["Error"] = $"Kişi sayısı 1 ile {table.TableCapacity} arasında olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            if (!TimeSpan.TryParse(reservationTime, out TimeSpan parsedTime))
            {
                TempData["Error"] = "Geçerli bir rezervasyon saati giriniz.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ UTC olarak oluştur
            var reservationDateTime = DateTime.UtcNow.Date.Add(parsedTime);

            if (reservationDateTime < DateTime.UtcNow)
            {
                TempData["Error"] = "Rezervasyon saati geçmiş bir saat olamaz.";
                return RedirectToAction(nameof(Index));
            }

            table.TableStatus = 2;
            table.ReservationName = reservationName.Trim();
            table.ReservationPhone = reservationPhone.Trim();
            table.ReservationGuestCount = reservationGuestCount;
            table.ReservationTime = reservationDateTime; // ✅ UTC

            await _db.SaveChangesAsync();

            TempData["Success"] = $"'{table.TableName}' — {reservationName} adına rezerve edildi.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Tables/CancelReserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReserve(int tableId)
        {
            var table = await _db.Tables.FindAsync(tableId);

            if (table == null)
            {
                TempData["Error"] = "Masa bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (table.TableStatus != 2)
            {
                TempData["Error"] = "Bu masa zaten rezerve değil.";
                return RedirectToAction(nameof(Index));
            }

            table.TableStatus = 0;
            table.ReservationName = null;
            table.ReservationPhone = null;
            table.ReservationGuestCount = null;
            table.ReservationTime = null;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"'{table.TableName}' rezervasyonu iptal edildi.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Tables/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int tableId)
        {
            var table = await _db.Tables.FindAsync(tableId);

            if (table == null)
            {
                TempData["Error"] = "Masa bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Dolu masayı silme
            if (table.TableStatus == 1)
            {
                TempData["Error"] = "Açık adisyonu olan masa silinemez.";
                return RedirectToAction(nameof(Index));
            }

            _db.Tables.Remove(table);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"'{table.TableName}' silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}