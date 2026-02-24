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
            // Sayfa her açılışında süresi dolan rezervasyonları temizle
            await CleanupExpiredReservationsAsync();

            ViewData["Title"] = "Masalar";
            ViewData["OccupiedCount"] = await _db.Tables
                                            .CountAsync(t => t.TableStatus == 1);

            var tables = await _db.Tables
                                  .OrderBy(t => t.TableName)
                                  .ToListAsync();

            return View(tables);
        }

        // Süresi dolan rezervasyonları temizle
        private async Task CleanupExpiredReservationsAsync()
        {
            var cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(30));

            var expired = await _db.Tables
                .Where(t => t.TableStatus == 2
                         && t.ReservationTime.HasValue
                         && t.ReservationTime.Value <= cutoff)
                .ToListAsync();

            if (!expired.Any()) return;

            foreach (var table in expired)
            {
                table.TableStatus = 0;
                table.ReservationName = null;
                table.ReservationPhone = null;
                table.ReservationGuestCount = null;
                table.ReservationTime = null;
            }

            await _db.SaveChangesAsync();
        }

        // POST /Tables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tableName, int tableCapacity)
        {
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

            var exists = await _db.Tables.AnyAsync(t => t.TableName == tableName.Trim());
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

            // ✅ FIX: Local time ile çalış (UTC+3 Türkiye saati)
            // DateTime.UtcNow.Date.Add() yerine DateTime.Now.Date.Add() kullan
            var localNow = DateTime.Now;
            var reservationLocal = localNow.Date.Add(parsedTime);

            // 5 dakika toleranslı geçmiş saat kontrolü
            if (reservationLocal < localNow.AddMinutes(-5))
            {
                TempData["Error"] = "Rezervasyon saati geçmiş bir saat olamaz.";
                return RedirectToAction(nameof(Index));
            }

            // DB'ye UTC olarak kaydet (PostgreSQL timestamptz uyumlu)
            var reservationUtc = DateTime.SpecifyKind(reservationLocal, DateTimeKind.Local)
                                         .ToUniversalTime();

            table.TableStatus = 2;
            table.ReservationName = reservationName.Trim();
            table.ReservationPhone = reservationPhone.Trim();
            table.ReservationGuestCount = reservationGuestCount;
            table.ReservationTime = reservationUtc;

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