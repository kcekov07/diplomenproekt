using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


namespace EcoLoop.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<StoreController> _logger;
        private readonly IWebHostEnvironment _env;
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

        public StoreController(ApplicationDbContext db, ILogger<StoreController> logger, IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
        }

        // GET: /Store/All
        public async Task<IActionResult> All()
        {
            var stores = await _db.Stores
                .Where(s => s.IsApproved)
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
                    ImageUrl = s.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault(),

                    // NEW (само ако ги добавиш в StoreViewModel)
                    HasDelivery = s.HasDelivery,
                    HasRefillStation = s.HasRefillStation,
                    EcoTags = s.EcoTags
                })
                .ToListAsync();

            return View(stores);
        }

        // GET: /Store/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var store = await _db.Stores
    .Include(s => s.Images)
    .Include(s => s.Phones)
    .Include(s => s.Comments)
    .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

            // visitorKey (ако има)
            var visitorKey = Request.Cookies.TryGetValue("ecoloop_vid", out var vk) ? vk : null;

            // Likes count per comment
            var commentIds = store.Comments.Select(c => c.Id).ToList();

            var likesDict = await _db.CommentHelpfuls
                .Where(h => commentIds.Contains(h.CommentId))
                .GroupBy(h => h.CommentId)
                .Select(g => new { CommentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CommentId, x => x.Count);

            ViewBag.CommentLikes = likesDict;

            // Which comments this visitor liked
            if (!string.IsNullOrWhiteSpace(visitorKey))
            {
                var likedIds = await _db.CommentHelpfuls
                    .Where(h => commentIds.Contains(h.CommentId) && h.VisitorKey == visitorKey)
                    .Select(h => h.CommentId)
                    .ToListAsync();

                ViewBag.LikedCommentIds = likedIds.ToHashSet();
            }
            else
            {
                ViewBag.LikedCommentIds = new HashSet<int>();
            }

            // Which comments can be edited by this visitor (by edit-token cookies)
            var canEdit = new HashSet<int>();
            foreach (var c in store.Comments)
            {
                if (Request.Cookies.TryGetValue($"ecoloop_edit_{c.Id}", out var token) && token == c.EditToken)
                    canEdit.Add(c.Id);
            }
            ViewBag.CanEditCommentIds = canEdit;

            return View(store);


        }

        // GET: /Store/Add
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

        // POST: /Store/Add
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
                return View(model);

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

                // NEW
                EcoTags = string.IsNullOrWhiteSpace(model.EcoTags) ? null : model.EcoTags.Trim(),
                Certifications = string.IsNullOrWhiteSpace(model.Certifications) ? null : model.Certifications.Trim(),
                HasDelivery = model.HasDelivery,
                HasRefillStation = model.HasRefillStation,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl.Trim(),
                FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl.Trim(),

                WorkingHours = BuildWorkingHours(model.MonToFriHours, model.SatHours, model.SunHours),
                Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim(),
                IsApproved = true,
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

                // Photos - save under wwwroot/images/stores/{id}/
                if (model.Photos != null && model.Photos.Any())
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsRoot = Path.Combine(webRoot, "images", "stores", store.Id.ToString());
                    Directory.CreateDirectory(uploadsRoot);

                    var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };

                    foreach (var file in model.Photos)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (file.Length > MaxFileBytes) continue;
                        if (!permitted.Contains(file.ContentType)) continue;

                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsRoot, fileName);

                        await using var stream = System.IO.File.Create(filePath);
                        await file.CopyToAsync(stream);

                        var url = $"/images/stores/{store.Id}/{fileName}";
                        _db.StoreImages.Add(new StoreImage { StoreId = store.Id, FileName = fileName, Url = url });
                    }
                }

                await _db.SaveChangesAsync();

                TempData["Message"] = "Магазинът е добавен успешно.";
                return RedirectToAction("Details", new { id = store.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                ModelState.AddModelError(string.Empty, "Възникна грешка при запис. Моля опитайте пак.");
                return View(model);
            }
        }

        // GET Edit: optional id; also returns list for dropdown if needed
        public async Task<IActionResult> Edit(int? id)
        {
            ViewData["Categories"] = new[] {
                "Еко храни🥕",
                "Натурална козметика🧴",
                "Еко облекло👕",
                "Еко автомобили🚗",
                "Еко продукти за дома🧼"
            };

            var allStores = await _db.Stores.OrderBy(s => s.Name).Select(s => new { s.Id, s.Name }).ToListAsync();
            ViewData["AllStores"] = allStores;

            if (!id.HasValue) return View(new StoreEditViewModel());

            var store = await _db.Stores
                .Include(s => s.Images)
                .Include(s => s.Phones)
                .FirstOrDefaultAsync(s => s.Id == id.Value);

            if (store == null) return NotFound();

            // try to split working hours
            string? mon = null, sat = null, sun = null;
            if (!string.IsNullOrWhiteSpace(store.WorkingHours))
            {
                var parts = store.WorkingHours.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                {
                    if (p.StartsWith("Пон-Пет:", StringComparison.OrdinalIgnoreCase)) mon = p.Split(':', 2)[1].Trim();
                    else if (p.StartsWith("Съб:", StringComparison.OrdinalIgnoreCase)) sat = p.Split(':', 2)[1].Trim();
                    else if (p.StartsWith("Нед:", StringComparison.OrdinalIgnoreCase)) sun = p.Split(':', 2)[1].Trim();
                }
            }

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
                MonToFriHours = mon,
                SatHours = sat,
                SunHours = sun,
                Website = store.Website,

                // NEW
                EcoTags = store.EcoTags,
                Certifications = store.Certifications,
                HasDelivery = store.HasDelivery,
                HasRefillStation = store.HasRefillStation,
                Email = store.Email,
                InstagramUrl = store.InstagramUrl,
                FacebookUrl = store.FacebookUrl,

                Phones = store.Phones?.Select(p => p.PhoneNumber).ToList() ?? new List<string>(),
                ExistingImages = store.Images?.Select(i => new StoreEditViewModel.ExistingImageViewModel { Id = i.Id, Url = i.Url })
                    .ToList() ?? new List<StoreEditViewModel.ExistingImageViewModel>()
            };

            return View(vm);
        }

        // POST Edit
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

            var allStores = await _db.Stores.OrderBy(s => s.Name).Select(s => new { s.Id, s.Name }).ToListAsync();
            ViewData["AllStores"] = allStores;

            if (!ModelState.IsValid) return View(model);

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
            store.WorkingHours = BuildWorkingHours(model.MonToFriHours, model.SatHours, model.SunHours);
            store.Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();

            // NEW
            store.EcoTags = string.IsNullOrWhiteSpace(model.EcoTags) ? null : model.EcoTags.Trim();
            store.Certifications = string.IsNullOrWhiteSpace(model.Certifications) ? null : model.Certifications.Trim();
            store.HasDelivery = model.HasDelivery;
            store.HasRefillStation = model.HasRefillStation;
            store.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            store.InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl.Trim();
            store.FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl.Trim();

            try
            {
                // replace phones
                var existingPhones = await _db.StorePhones.Where(p => p.StoreId == store.Id).ToListAsync();
                if (existingPhones.Any()) _db.StorePhones.RemoveRange(existingPhones);

                if (model.Phones != null)
                {
                    foreach (var raw in model.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                        _db.StorePhones.Add(new StorePhone { StoreId = store.Id, PhoneNumber = raw.Trim() });
                }

                // append photos to wwwroot/images/stores/{id}
                if (model.Photos != null && model.Photos.Any())
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsRoot = Path.Combine(webRoot, "images", "stores", store.Id.ToString());
                    Directory.CreateDirectory(uploadsRoot);

                    var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    foreach (var file in model.Photos)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (file.Length > MaxFileBytes) continue;
                        if (!permitted.Contains(file.ContentType)) continue;

                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsRoot, fileName);

                        await using var stream = System.IO.File.Create(filePath);
                        await file.CopyToAsync(stream);

                        var url = $"/images/stores/{store.Id}/{fileName}";
                        _db.StoreImages.Add(new StoreImage { StoreId = store.Id, FileName = fileName, Url = url });
                    }
                }

                await _db.SaveChangesAsync();

                TempData["Message"] = "Промените са записани успешно.";
                return RedirectToAction("Details", new { id = store.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store {Id}", store.Id);
                ModelState.AddModelError(string.Empty, "Възникна грешка при обновяване. Моля опитайте пак.");
                return View(model);
            }
        }

        // POST: delete image (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var img = await _db.StoreImages.FindAsync(imageId);
            if (img == null) return Json(new { ok = false, error = "not_found" });

            try
            {
                _db.StoreImages.Remove(img);
                await _db.SaveChangesAsync();

                // delete file from disk (image url stored like "/images/stores/{id}/{file}")
                try
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var relative = img.Url?.TrimStart('/') ?? string.Empty;
                    var filePath = Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }
                catch (Exception fx)
                {
                    _logger.LogWarning(fx, "Failed to delete file for image {Id}", imageId);
                }

                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {Id}", imageId);
                return Json(new { ok = false, error = "delete_failed" });
            }
        }

        private static string? BuildWorkingHours(string? monToFri, string? sat, string? sun)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(monToFri)) parts.Add($"Пон-Пет: {monToFri.Trim()}");
            if (!string.IsNullOrWhiteSpace(sat)) parts.Add($"Съб: {sat.Trim()}");
            if (!string.IsNullOrWhiteSpace(sun)) parts.Add($"Нед: {sun.Trim()}");
            return parts.Count == 0 ? null : string.Join("; ", parts);
        }
    }
}
