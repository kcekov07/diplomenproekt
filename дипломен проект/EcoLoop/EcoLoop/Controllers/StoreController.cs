using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EcoLoop.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<StoreController> _logger;
        private readonly IWebHostEnvironment _env;

        // Limit per-file size (bytes)
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

        public StoreController(
            ApplicationDbContext db,
            ILogger<StoreController> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
        }

        public async Task<IActionResult> All()
        {
            var stores = await _db.Stores
                .Where(s => s.Approved)
                .OrderByDescending(s => s.Rating)
                .Select(s => new StoreViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    ShortDescription = s.ShortDescription,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Rating = s.Rating,
                    ImageUrl = s.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
                })
                .ToListAsync();

            return View(stores);
        }

        public async Task<IActionResult> Details(int id)
        {
            var store = await _db.Stores
                .Include(s => s.Images)
                .Include(s => s.Phones)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

            return View(store);
        }

        public IActionResult Add()
        {
            ViewData["Categories"] = new[] {
                "Еко храни🥕",
                "Натурална козметика🧴",
                "Еко облекло👕",
                "Еко автомобили🚗",
                "Еко продукти за дома🧼"
            };

            return View(new StoreAddViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(StoreAddViewModel model)
        {
            ViewData["Categories"] = new[] {
                "Еко храни🥕",
                "Натурална козметика🧴",
                "Еко облекло👕",
                "Еко автомобили🚗",
                "Еко продукти за дома🧼"
            };

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var store = new Store
            {
                Name = model.Name?.Trim() ?? string.Empty,
                Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category,
                ShortDescription = !string.IsNullOrWhiteSpace(model.ShortDescription)
                    ? model.ShortDescription.Trim()
                    : (model.Description != null && model.Description.Length > 200 ? model.Description[..200] : model.Description),
                Description = model.Description,
                Address = model.Address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                AcceptsOwnPackaging = model.AcceptsOwnPackaging,
                IsProducer = model.IsProducer,
                WorkingHours = BuildWorkingHours(model),


                Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim(),
                Approved = true,
                Rating = 0m
            };

            try
            {
                _db.Stores.Add(store);
                await _db.SaveChangesAsync();

                // Phones
                if (model.Phones != null)
                {
                    foreach (var raw in model.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                    {
                        _db.StorePhones.Add(new StorePhone { StoreId = store.Id, PhoneNumber = raw.Trim() });
                    }
                }

                // Photos
                if (model.Photos != null && model.Photos.Any())
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsRoot = Path.Combine(webRoot, "uploads", "stores", store.Id.ToString());
                    Directory.CreateDirectory(uploadsRoot);

                    foreach (var file in model.Photos)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (file.Length > MaxFileBytes) continue;
                        var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                        if (!permitted.Contains(file.ContentType)) continue;

                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsRoot, fileName);

                        await using var stream = System.IO.File.Create(filePath);
                        await file.CopyToAsync(stream);

                        var url = $"/uploads/stores/{store.Id}/{fileName}";
                        _db.StoreImages.Add(new StoreImage { StoreId = store.Id, FileName = fileName, Url = url });
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Message"] = "Магазинът е добавен успешно.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                ModelState.AddModelError(string.Empty, "Възникна грешка при запис. Моля опитайте пак.");
                return View(model);
            }
        }

        // GET: Store/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var store = await _db.Stores
                .Include(s => s.Images)
                .Include(s => s.Phones)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

            ViewData["Categories"] = new[] {
                "Еко храни🥕",
                "Натурална козметика🧴",
                "Еко облекло👕",
                "Еко автомобили🚗",
                "Еко продукти за дома🧼"
            };

            var vm = new StoreEditViewModel
            {
                Id = store.Id,
                Name = store.Name,
                Category = store.Category,
                ShortDescription = store.ShortDescription,
                Description = store.Description,
                Address = store.Address,
                Latitude = store.Latitude,
                Longitude = store.Longitude,
                AcceptsOwnPackaging = store.AcceptsOwnPackaging,
                IsProducer = store.IsProducer,
                WorkingHours = store.WorkingHours,
                Website = store.Website,
                Phones = store.Phones?.Select(p => p.PhoneNumber).ToList() ?? new List<string>(),
                ExistingImages = store.Images?.Select(i => new StoreEditViewModel.ExistingImageViewModel { Id = i.Id, Url = i.Url }).ToList() ?? new List<StoreEditViewModel.ExistingImageViewModel>()
            };

            return View(vm);
        }

        // POST: Store/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StoreEditViewModel model)
        {
            ViewData["Categories"] = new[] {
                "Еко храни🥕",
                "Натурална козметика🧴",
                "Еко облекло👕",
                "Еко автомобили🚗",
                "Еко продукти за дома🧼"
            };

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var store = await _db.Stores
                .Include(s => s.Images)
                .Include(s => s.Phones)
                .FirstOrDefaultAsync(s => s.Id == model.Id);

            if (store == null) return NotFound();

            store.Name = model.Name?.Trim() ?? string.Empty;
            store.Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category;
            store.ShortDescription = !string.IsNullOrWhiteSpace(model.ShortDescription)
                ? model.ShortDescription.Trim()
                : (model.Description != null && model.Description.Length > 200 ? model.Description[..200] : model.Description);
            store.Description = model.Description;
            store.Address = model.Address;
            store.Latitude = model.Latitude;
            store.Longitude = model.Longitude;
            store.AcceptsOwnPackaging = model.AcceptsOwnPackaging;
            store.IsProducer = model.IsProducer;
            store.WorkingHours = BuildWorkingHours(new StoreAddViewModel
            {
                MonToFriHours = model.MonToFriHours,
                SatHours = model.SatHours,
                SunHours = model.SunHours
            });
            store.Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();

            try
            {
                // Replace phones: remove existing then add new
                var existingPhones = await _db.StorePhones.Where(p => p.StoreId == store.Id).ToListAsync();
                if (existingPhones.Any())
                {
                    _db.StorePhones.RemoveRange(existingPhones);
                }

                if (model.Phones != null)
                {
                    foreach (var raw in model.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                    {
                        _db.StorePhones.Add(new StorePhone { StoreId = store.Id, PhoneNumber = raw.Trim() });
                    }
                }

                // Handle new photo uploads (append only)
                if (model.Photos != null && model.Photos.Any())
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsRoot = Path.Combine(webRoot, "uploads", "stores", store.Id.ToString());
                    Directory.CreateDirectory(uploadsRoot);

                    foreach (var file in model.Photos)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (file.Length > MaxFileBytes) continue;
                        var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                        if (!permitted.Contains(file.ContentType)) continue;

                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsRoot, fileName);

                        await using var stream = System.IO.File.Create(filePath);
                        await file.CopyToAsync(stream);

                        var url = $"/uploads/stores/{store.Id}/{fileName}";
                        _db.StoreImages.Add(new StoreImage { StoreId = store.Id, FileName = fileName, Url = url });
                    }
                }

                await _db.SaveChangesAsync();

                TempData["Message"] = "Магазинът е обновен успешно.";
                return RedirectToAction(nameof(Details), new { id = store.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store {Id}", store.Id);
                ModelState.AddModelError(string.Empty, "Възникна грешка при обновяване. Моля опитайте пак.");
                return View(model);
            }
        }

        // Helper to rollback partial store creation (used earlier in Add)
        private async Task RollbackStoreAsync(int storeId)
        {
            if (storeId <= 0) return;
            try
            {
                var s = await _db.Stores.FindAsync(storeId);
                if (s != null)
                {
                    _db.Stores.Remove(s);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rollback failed for store {Id}", storeId);
            }
        }

        // Add this private helper method to StoreController
        private static string? BuildWorkingHours(StoreAddViewModel model)
        {
            if (model == null)
            {
                return null;
            }

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(model.MonToFriHours))
            {
                parts.Add($"Пон-Пет: {model.MonToFriHours.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(model.SatHours))
            {
                parts.Add($"Съб: {model.SatHours.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(model.SunHours))
            {
                parts.Add($"Нед: {model.SunHours.Trim()}");
            }

            return parts.Count > 0 ? string.Join("; ", parts) : null;
        }
    }
}