using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Menu;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using System.Globalization;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MenuController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> _allowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public MenuController(RestaurantDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Menü Ürünleri";

            // ✅ IsDeleted filtresi — soft delete edilmişleri gösterme
           
            var menuItems = await _context.MenuItems
                .Where(m => !m.IsDeleted)
                .Include(m => m.Category)
                .OrderBy(m => m.Category.CategorySortOrder)

                .ThenBy(m => m.MenuItemName)
                .ToListAsync();
            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)

                .ThenBy(c => c.CategoryName)
                .ToListAsync();
            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => !m.IsDeleted && m.TrackStock && m.StockQuantity < 5);
            return View(menuItems);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);
            if (item == null) return NotFound();
            ViewData["Title"] = $"{item.MenuItemName} — Detay";
            ViewData["HasLowStock"] = await _context.MenuItems
                .AnyAsync(m => !m.IsDeleted && m.TrackStock && m.StockQuantity < 5);
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Yeni Ürün";
            ViewData["Categories"] = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)

                .ToListAsync();



        // ── POST: /Menu/Create  (AJAX JSON) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
            if (!decimal.TryParse(
                    dto.MenuItemPriceStr?.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal menuItemPrice) || menuItemPrice < 0)
            bool catExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!catExists)
            string? imagePath = null;
            if (imageFile != null && imageFile.Length > 0)
                   
                MenuItemName = dto.MenuItemName.Trim(),
                NameEn = string.IsNullOrWhiteSpace(dto.NameEn) ? null : dto.NameEn.Trim(),
                NameAr = string.IsNullOrWhiteSpace(dto.NameAr) ? null : dto.NameAr.Trim(),
                NameRu = string.IsNullOrWhiteSpace(dto.NameRu) ? null : dto.NameRu.Trim(),
                CategoryId = dto.CategoryId,
                MenuItemPrice = menuItemPrice,
                Description = dto.Description?.Trim() ?? string.Empty,
                DescriptionEn = string.IsNullOrWhiteSpace(dto.DescriptionEn) ? null : dto.DescriptionEn.Trim(),
                DescriptionAr = string.IsNullOrWhiteSpace(dto.DescriptionAr) ? null : dto.DescriptionAr.Trim(),
                DescriptionRu = string.IsNullOrWhiteSpace(dto.DescriptionRu) ? null : dto.DescriptionRu.Trim(),
                StockQuantity = dto.StockQuantity,
                TrackStock = dto.TrackStock,
                IsAvailable = dto.IsAvailable,
                
                ImagePath = imagePath,
                StockQuantity = stockQuantity,
                TrackStock = trackStock,
                IsAvailable = isAvailable,

        // ── GET: /Menu/Edit/5 ────────────────────────────────────────
                TrackStock = trackStock,
                IsAvailable = isAvailable,
                IsAvailable = dto.IsAvailable,
                IsDeleted = false,
                MenuItemCreatedTime = DateTime.UtcNow
        // ── GET: /Menu/Edit/5 ────────────────────────────────────────
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ürün başarıyla eklendi." });
        }
        // ── GET: /Menu/Edit/5 ────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
        // ── POST: /Menu/Edit  (AJAX JSON) ────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] MenuItemEditDto dto)
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategorySortOrder)
        // ── POST: /Menu/Edit  (AJAX JSON) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
          public async Task<IActionResult> Edit([FromBody] MenuItemEditDto dto)
        // ── POST: /Menu/Edit  (AJAX JSON) ────────────────────────────
        [HttpPost]
            if (!decimal.TryParse(
                    dto.MenuItemPriceStr?.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal menuItemPrice) || menuItemPrice < 0)
                return Json(new { success = false, message = "Ürün bulunamadı." });
            // ✅ FIX #3: Virgül/nokta parse
            if (!decimal.TryParse(
                    dto.MenuItemPriceStr?.Replace(',', '.'),
                    NumberStyles.Any,
                    if (removeImage && item.ImagePath != null)
            {
                DeleteImageFile(item.ImagePath);
    item.ImagePath = null;
            }
            else if (imageFile != null && imageFile.Length > 0)
{
    if (item.ImagePath != null) DeleteImageFile(item.ImagePath);
    var (path, err) = await SaveImageAsync(imageFile);
    if (err != null) return Json(new { success = false, message = err });
    item.ImagePath = path;
}
item.MenuItemName = dto.MenuItemName.Trim();
            item.NameEn = string.IsNullOrWhiteSpace(dto.NameEn) ? null : dto.NameEn.Trim();
            item.NameAr = string.IsNullOrWhiteSpace(dto.NameAr) ? null : dto.NameAr.Trim();
            item.NameRu = string.IsNullOrWhiteSpace(dto.NameRu) ? null : dto.NameRu.Trim();
            item.CategoryId = dto.CategoryId;
            item.MenuItemPrice = menuItemPrice;
            item.Description = dto.Description?.Trim() ?? string.Empty;
            item.DescriptionEn = string.IsNullOrWhiteSpace(dto.DescriptionEn) ? null : dto.DescriptionEn.Trim();
            item.DescriptionAr = string.IsNullOrWhiteSpace(dto.DescriptionAr) ? null : dto.DescriptionAr.Trim();
            item.DescriptionRu = string.IsNullOrWhiteSpace(dto.DescriptionRu) ? null : dto.DescriptionRu.Trim();
            item.StockQuantity = dto.StockQuantity;
            item.TrackStock = dto.TrackStock;
            item.IsAvailable = dto.IsAvailable;
            item.MenuItemName = dto.MenuItemName.Trim();
            item.CategoryId = dto.CategoryId;
            item.MenuItemPrice = menuItemPrice;
            item.Description = dto.Description?.Trim() ?? string.Empty;

        // ── POST: /Menu/Delete  (AJAX JSON) ──────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
            item.Description = dto.Description?.Trim() ?? string.Empty;
            item.StockQuantity = dto.StockQuantity;
            item.TrackStock = dto.TrackStock;
            if (item == null)
                return Json(new { success = false, message = "Ürün bulunamadı." });

            bool usedInOrders = await _context.OrderItems
                .AnyAsync(oi => oi.MenuItemId == id);
        // ── POST: /Menu/Delete  (AJAX JSON) ──────────────────────────
        //            Hiç kullanılmadıysa → fiziksel sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ✅ FIX #2: Siparişlerde kullanıldıysa → soft delete (IsDeleted=true)
        //            Hiç kullanılmadıysa → fiziksel sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
            if (item.ImagePath != null) DeleteImageFile(item.ImagePath);
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Ürün bulunamadı." });

            bool usedInOrders = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == id);
            if (usedInOrders)
            {
                item.IsDeleted = true;
                item.IsAvailable = false;
            // Hiç sipariş yok → fiziksel sil
                return Json(new { success = true, message = "Ürün pasife alındı (geçmiş siparişlerde kullanılmış)." });
            }


            // Hiç sipariş yok → fiziksel sil
            _context.MenuItems.Remove(item);
        // ── GET: /Menu/GetById/5  (Edit modal için) ──────────────────
            return Json(new { success = true, message = "Ürün silindi." });
        }

        // ── GET: /Menu/GetById/5  (Edit modal için) ──────────────────
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
                description = m.Description ?? "",
                descriptionEn = m.DescriptionEn ?? "",
                descriptionAr = m.DescriptionAr ?? "",
                descriptionRu = m.DescriptionRu ?? "",
            var m = await _context.MenuItems.FindAsync(id);
            if (m == null) return Json(new { success = false });
            return Json(new
            {
                success = true,
                menuItemId = m.MenuItemId,
                menuItemName = m.MenuItemName,
                nameEn = m.NameEn ?? "",
                description = m.Description,
                nameRu = m.NameRu ?? "",
                categoryId = m.CategoryId,
                menuItemPrice = m.MenuItemPrice.ToString("F2", CultureInfo.InvariantCulture),
                description = m.Description,
                stockQuantity = m.StockQuantity,
                trackStock = m.TrackStock,
                isAvailable = m.IsAvailable,
                imagePath = m.ImagePath
            });
        }

        private async Task<(string? path, string? error)> SaveImageAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(ext))
                return (null, "Geçersiz dosya türü. Yalnızca JPG, PNG, WEBP veya GIF yüklenebilir.");
            if (file.Length > MaxFileSizeBytes)
                return (null, "Dosya boyutu 5 MB'ı geçemez.");

            var folder = Path.Combine(_env.WebRootPath, "images", "menu");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var fullPath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return ($"/images/menu/{fileName}", null);
        }

        private void DeleteImageFile(string relativePath)
        {
            try
            {
                var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch { }
        }
    }
}
