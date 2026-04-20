using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Authorization;
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
        private const long MaxFileBytes = 5 * 1024 * 1024; 

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

                    
                    HasDelivery = s.HasDelivery,
                    HasRefillStation = s.HasRefillStation,
                    EcoTags = s.EcoTags
                })
                .ToListAsync();

            return View(stores);
        }

        // GET: /Store/Details/
        public async Task<IActionResult> Details(int id)
        {
            var store = await _db.Stores
    .Include(s => s.Images)
    .Include(s => s.Phones)
    .Include(s => s.Comments)
    .Include(s => s.Products)
    .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

           
            var visitorKey = Request.Cookies.TryGetValue("ecoloop_vid", out var vk) ? vk : null;

            
            var commentIds = store.Comments.Select(c => c.Id).ToList();

            var likesDict = await _db.CommentHelpfuls
                .Where(h => commentIds.Contains(h.CommentId))
                .GroupBy(h => h.CommentId)
                .Select(g => new { CommentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CommentId, x => x.Count);

            ViewBag.CommentLikes = likesDict;

            
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
            
            var canEdit = new HashSet<int>();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                foreach (var c in store.Comments)
                {
                    if (c.UserId == currentUserId)
                        canEdit.Add(c.Id);
                }
            }
            ViewBag.CanEditCommentIds = canEdit;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var visitedExists = await _db.UserVisitedStores.AnyAsync(x => x.UserId == userId && x.StoreId == id);
                    if (!visitedExists)
                    {
                        _db.UserVisitedStores.Add(new UserVisitedStore { UserId = userId, StoreId = id });
                        await _db.SaveChangesAsync();
                    }

                    ViewBag.IsFavorite = await _db.UserFavoriteStores.AnyAsync(x => x.UserId == userId && x.StoreId == id);
                    ViewBag.CartCount = await _db.CartItems.Where(x => x.UserId == userId).SumAsync(x => (decimal?)x.Quantity) ?? 0m;
                }
            }
            ViewBag.CanManageProducts = await CanManageProductsAsync(store);
            return View(store);


        }

        // GET: /Store/Add

        [Authorize(Roles = "Producer,Moderator,Admin")]
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
        [Authorize(Roles = "Producer,Moderator,Admin")]
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
                CreatorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                IsApproved = User.IsInRole("Admin") || User.IsInRole("Moderator"),

               
                EcoTags = string.IsNullOrWhiteSpace(model.EcoTags) ? null : model.EcoTags.Trim(),
                Certifications = string.IsNullOrWhiteSpace(model.Certifications) ? null : model.Certifications.Trim(),
                HasDelivery = model.HasDelivery,
                HasRefillStation = model.HasRefillStation,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl.Trim(),
                FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl.Trim(),

                WorkingHours = BuildWorkingHours(model.MonToFriHours, model.SatHours, model.SunHours),
                Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim(),
                
                Rating = 0m
            };

            try
            {
                _db.Stores.Add(store);
                await _db.SaveChangesAsync();

                
                if (model.Phones != null)
                {
                    foreach (var raw in model.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                    {
                        _db.StorePhones.Add(new StorePhone { StoreId = store.Id, PhoneNumber = raw.Trim() });
                    }
                }

                
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

        // GET Edit
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isElevated = User.IsInRole("Admin") || User.IsInRole("Moderator");
            if (!isElevated && store.CreatorId != userId) return Forbid();

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

            
            store.EcoTags = string.IsNullOrWhiteSpace(model.EcoTags) ? null : model.EcoTags.Trim();
            store.Certifications = string.IsNullOrWhiteSpace(model.Certifications) ? null : model.Certifications.Trim();
            store.HasDelivery = model.HasDelivery;
            store.HasRefillStation = model.HasRefillStation;
            store.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            store.InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl.Trim();
            store.FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl.Trim();

            try
            {
                
                var existingPhones = await _db.StorePhones.Where(p => p.StoreId == store.Id).ToListAsync();
                if (existingPhones.Any()) _db.StorePhones.RemoveRange(existingPhones);

                if (model.Phones != null)
                {
                    foreach (var raw in model.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                        _db.StorePhones.Add(new StorePhone { StoreId = store.Id, PhoneNumber = raw.Trim() });
                }

                
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

        // POST: delete image 
        [Authorize(Roles = "Producer,Moderator,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var img = await _db.StoreImages.FindAsync(imageId);
            if (img == null) return Json(new { ok = false, error = "not_found" });

            var store = await _db.Stores.FindAsync(img.StoreId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isElevated = User.IsInRole("Admin") || User.IsInRole("Moderator");
            if (store == null || (!isElevated && store.CreatorId != userId)) return Forbid();

            try
            {
                _db.StoreImages.Remove(img);
                await _db.SaveChangesAsync();

                
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

        [Authorize(Roles = "Producer,Moderator,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var store = await _db.Stores.FindAsync(id);
            if (store == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isElevated = User.IsInRole("Admin") || User.IsInRole("Moderator");
            if (!isElevated && store.CreatorId != userId)
            {
                return Forbid();
            }

            _db.Stores.Remove(store);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var fav = await _db.UserFavoriteStores.FirstOrDefaultAsync(x => x.UserId == userId && x.StoreId == id);
            if (fav == null)
            {
                _db.UserFavoriteStores.Add(new UserFavoriteStore { UserId = userId, StoreId = id });
            }
            else
            {
                _db.UserFavoriteStores.Remove(fav);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
        [Authorize]
        public async Task<IActionResult> Catalog(int id)
        {
            var store = await _db.Stores
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

            ViewBag.CanManageProducts = await CanManageProductsAsync(store);
            ViewBag.CartCount = 0;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    ViewBag.CartCount = await _db.CartItems.Where(x => x.UserId == userId).SumAsync(x => (decimal?)x.Quantity) ?? 0m;
                }
            }

            return View(store);
        }

        [Authorize]
        public async Task<IActionResult> AddProduct(int storeId)
        {
            var store = await _db.Stores.FirstOrDefaultAsync(x => x.Id == storeId);
            if (store == null) return NotFound();
            if (!await CanManageProductsAsync(store)) return Forbid();

            ViewBag.StoreName = store.Name;
            return View(new StoreProductInputModel { StoreId = storeId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(StoreProductInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["CatalogError"] = "Попълни валидно всички задължителни полета за продукта.";
                return RedirectToAction(nameof(AddProduct), new { storeId = model.StoreId });
            }

            var store = await _db.Stores.FirstOrDefaultAsync(x => x.Id == model.StoreId);
            if (store == null) return NotFound();
            if (!await CanManageProductsAsync(store)) return Forbid();

            string? imageUrl = null;
            var file = model.ProductImage;
            if (file is { Length: > 0 })
            {
                if (file.Length > MaxFileBytes)
                {
                    TempData["CatalogError"] = "Изображението е твърде голямо (макс 5MB).";
                    return RedirectToAction(nameof(AddProduct), new { storeId = model.StoreId });
                }

                var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!permitted.Contains(file.ContentType))
                {
                    TempData["CatalogError"] = "Позволени са само изображения JPG, PNG, GIF или WEBP.";
                    return RedirectToAction(nameof(AddProduct), new { storeId = model.StoreId });
                }

                var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsRoot = Path.Combine(webRoot, "images", "products", store.Id.ToString());
                Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);
                imageUrl = $"/images/products/{store.Id}/{fileName}";
            }
            var product = new StoreProduct
            {
                StoreId = store.Id,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Price = model.Price,
                ImageUrl = imageUrl,
                Unit = string.IsNullOrWhiteSpace(model.Unit) ? null : model.Unit.Trim(),
                Labels = string.IsNullOrWhiteSpace(model.Labels) ? null : model.Labels.Trim(),
                IsAvailable = model.IsAvailable
            };

            _db.StoreProducts.Add(product);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Продуктът е добавен в каталога.";
            return RedirectToAction(nameof(Catalog), new { id = model.StoreId });
        }
        [Authorize]
        public async Task<IActionResult> EditProduct(int id, int storeId)
        {
            var store = await _db.Stores.FirstOrDefaultAsync(x => x.Id == storeId);
            if (store == null) return NotFound();
            if (!await CanManageProductsAsync(store)) return Forbid();

            var product = await _db.StoreProducts.FirstOrDefaultAsync(x => x.Id == id && x.StoreId == storeId);
            if (product == null) return NotFound();

            ViewBag.StoreName = store.Name;
            ViewBag.ProductId = product.Id;
            ViewBag.CurrentImageUrl = product.ImageUrl;

            return View(new StoreProductInputModel
            {
                StoreId = product.StoreId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Unit = product.Unit,
                Labels = product.Labels,
                IsAvailable = product.IsAvailable
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, StoreProductInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["CatalogError"] = "Попълни валидно всички задължителни полета за продукта.";
                return RedirectToAction(nameof(EditProduct), new { id, storeId = model.StoreId });
            }

            var store = await _db.Stores.FirstOrDefaultAsync(x => x.Id == model.StoreId);
            if (store == null) return NotFound();
            if (!await CanManageProductsAsync(store)) return Forbid();

            var product = await _db.StoreProducts.FirstOrDefaultAsync(x => x.Id == id && x.StoreId == model.StoreId);
            if (product == null) return NotFound();

            var file = model.ProductImage;
            if (file is { Length: > 0 })
            {
                if (file.Length > MaxFileBytes)
                {
                    TempData["CatalogError"] = "Изображението е твърде голямо (макс 5MB).";
                    return RedirectToAction(nameof(EditProduct), new { id, storeId = model.StoreId });
                }

                var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!permitted.Contains(file.ContentType))
                {
                    TempData["CatalogError"] = "Позволени са само изображения JPG, PNG, GIF или WEBP.";
                    return RedirectToAction(nameof(EditProduct), new { id, storeId = model.StoreId });
                }

                var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsRoot = Path.Combine(webRoot, "images", "products", store.Id.ToString());
                Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);

                if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    try
                    {
                        var oldRelative = product.ImageUrl.TrimStart('/');
                        var oldPath = Path.Combine(webRoot, oldRelative.Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete old image for product {ProductId}", id);
                    }
                }

                product.ImageUrl = $"/images/products/{store.Id}/{fileName}";
            }

            product.Name = model.Name.Trim();
            product.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            product.Price = model.Price;
            product.Unit = string.IsNullOrWhiteSpace(model.Unit) ? null : model.Unit.Trim();
            product.Labels = string.IsNullOrWhiteSpace(model.Labels) ? null : model.Labels.Trim();
            product.IsAvailable = model.IsAvailable;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Продуктът е редактиран успешно.";
            return RedirectToAction(nameof(Catalog), new { id = model.StoreId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id, int storeId)
        {
            var store = await _db.Stores.FirstOrDefaultAsync(x => x.Id == storeId);
            if (store == null) return NotFound();
            if (!await CanManageProductsAsync(store)) return Forbid();

            var product = await _db.StoreProducts.FirstOrDefaultAsync(x => x.Id == id && x.StoreId == storeId);
            if (product == null) return NotFound();

            _db.StoreProducts.Remove(product);
            await _db.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                try
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var relative = product.ImageUrl.TrimStart('/');
                    var filePath = Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete image for product {ProductId}", id);
                }
            }

            TempData["Message"] = "Продуктът е премахнат от каталога.";
            return RedirectToAction(nameof(Catalog), new { id = storeId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, decimal quantity = 1m)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            quantity = Math.Clamp(quantity, 0.1m, 20m);
            quantity = decimal.Round(quantity, 2, MidpointRounding.AwayFromZero);

            var product = await _db.StoreProducts.FirstOrDefaultAsync(x => x.Id == productId && x.IsAvailable);
            if (product == null) return NotFound();

            var existing = await _db.CartItems.FirstOrDefaultAsync(x => x.UserId == userId && x.StoreProductId == productId);
            if (existing == null)
            {
                _db.CartItems.Add(new CartItem { UserId = userId, StoreProductId = productId, Quantity = quantity });
            }
            else
            {
                existing.Quantity = Math.Clamp(existing.Quantity + quantity, 0.1m, 99m);
                existing.Quantity = decimal.Round(existing.Quantity, 2, MidpointRounding.AwayFromZero);
            }

            await _db.SaveChangesAsync();
            TempData["Message"] = "Продуктът е добавен в количката.";
            return RedirectToAction(nameof(Details), new { id = product.StoreId });
        }

        [Authorize]
        public async Task<IActionResult> Cart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var items = await _db.CartItems
                .Where(x => x.UserId == userId)
                .Include(x => x.StoreProduct)
                .ThenInclude(p => p.Store)
                .OrderByDescending(x => x.AddedOn)
                .ToListAsync();

            var vm = new CartViewModel
            {
                Items = items.Select(x => new CartLineViewModel
                {
                    CartItemId = x.Id,
                    ProductId = x.StoreProductId,
                    StoreId = x.StoreProduct.StoreId,
                    StoreName = x.StoreProduct.Store.Name,
                    ProductName = x.StoreProduct.Name,
                    ProductImageUrl = x.StoreProduct.ImageUrl,
                    UnitPrice = x.StoreProduct.Price,
                    Quantity = x.Quantity,
                    Unit = x.StoreProduct.Unit
                }).ToList()
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, decimal quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var item = await _db.CartItems
                .Include(x => x.StoreProduct)
                .FirstOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId);

            if (item == null) return NotFound();

            item.Quantity = Math.Clamp(quantity, 0.1m, 99m);
            item.Quantity = decimal.Round(item.Quantity, 2, MidpointRounding.AwayFromZero); await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Cart));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var item = await _db.CartItems.FirstOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId);
            if (item == null) return NotFound();

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Cart));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var items = await _db.CartItems.Where(x => x.UserId == userId).ToListAsync();
            if (!items.Any())
            {
                TempData["CartMessage"] = "Количката е празна.";
                return RedirectToAction(nameof(Cart));
            }

            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();

            TempData["CartMessage"] = "Поръчката е изпратена успешно. Магазините ще се свържат с теб за потвърждение.";
            return RedirectToAction(nameof(Cart));
        }

        private Task<bool> CanManageStoreAsync(Store store)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(false);
            }

            if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(store.CreatorId == userId);
        }

        private Task<bool> CanManageProductsAsync(Store store)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(store.CreatorId == userId);
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
