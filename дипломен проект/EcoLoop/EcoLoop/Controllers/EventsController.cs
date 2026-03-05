using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EcoLoop.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EventsController> _logger;
        private const long MaxFileBytes = 5 * 1024 * 1024;
        private static readonly string[] PermittedImageContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

        public EventsController(ApplicationDbContext db, IWebHostEnvironment env, ILogger<EventsController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }
        public async Task<IActionResult> All(DateTime? date, string? type, string? city)
        {
            var query = _db.Events.AsNoTracking().AsQueryable();

            if (date.HasValue)
            {
                var filterDate = date.Value.Date;
                query = query.Where(e => e.Date.Date == filterDate);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(e => e.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(e => e.City == city);
            }

            var model = new EventsIndexViewModel
            {
                DateFilter = date,
                TypeFilter = type,
                CityFilter = city,
                AvailableTypes = await _db.Events
                    .AsNoTracking()
                    .Where(e => e.Type != null && e.Type != string.Empty)
                    .Select(e => e.Type!)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync(),
                AvailableCities = await _db.Events
                    .AsNoTracking()
                    .Where(e => e.City != null && e.City != string.Empty)
                    .Select(e => e.City!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync(),
                Events = await query
                    .OrderBy(e => e.Date)
                    .Select(e => new EventViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Date = e.Date,
                        City = e.City,
                        Type = e.Type,
                        ImageUrl = e.ImageUrl,
                        ShortDescription = e.ShortDescription
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (item == null) return NotFound();

            return View(item);
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Create()
        {
            return View(new EventFormViewModel
            {
                AvailableTypes = await GetAvailableEventTypesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            model.Type = NormalizeEventType(model.Type, model.CustomType);
            ModelState.Remove(nameof(model.Type));
            ValidateEventImage(model.ImageFile);
            if (string.IsNullOrWhiteSpace(model.Type))
            {
                ModelState.AddModelError(nameof(model.Type), "Типът е задължителен");
            }
            if (!ModelState.IsValid)
            {
                model.AvailableTypes = await GetAvailableEventTypesAsync();
                return View(model);
            }

            var entity = new Event
            {
                Title = model.Title,
                Date = model.Date,
                City = model.City,
                Type = model.Type,
                ShortDescription = model.ShortDescription
            };

            _db.Events.Add(entity);
            await _db.SaveChangesAsync();
            entity.ImageUrl = await SaveEventImageAsync(entity.Id, model.ImageFile);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Events.FindAsync(id);
            if (item == null) return NotFound();

            var availableTypes = await GetAvailableEventTypesAsync();
            var existingType = item.Type ?? string.Empty;
            var hasExistingType = availableTypes.Contains(existingType);

            var model = new EventFormViewModel
            {
                Id = item.Id,
                Title = item.Title,
                ImageUrl = item.ImageUrl,
                ExistingImageUrl = item.ImageUrl,
                Date = item.Date,
                City = item.City ?? string.Empty,
                Type = existingType,
                CustomType = hasExistingType ? null : existingType,
                ShortDescription = item.ShortDescription ?? string.Empty,
                AvailableTypes = availableTypes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Edit(EventFormViewModel model)
        {
            model.Type = NormalizeEventType(model.Type, model.CustomType);
            ModelState.Remove(nameof(model.Type));
            ValidateEventImage(model.ImageFile);
            if (string.IsNullOrWhiteSpace(model.Type))
            {
                ModelState.AddModelError(nameof(model.Type), "Типът е задължителен");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableTypes = await GetAvailableEventTypesAsync();

                if (string.IsNullOrWhiteSpace(model.ExistingImageUrl))
                {
                    model.ExistingImageUrl = await _db.Events
                        .AsNoTracking()
                        .Where(e => e.Id == model.Id)
                        .Select(e => e.ImageUrl)
                        .FirstOrDefaultAsync();
                }

                return View(model);
            }

            var item = await _db.Events.FindAsync(model.Id);
            if (item == null) return NotFound();

            item.Title = model.Title;
            item.Date = model.Date;
            item.City = model.City;
            item.Type = model.Type;
            item.ShortDescription = model.ShortDescription;

            if (model.RemoveImage)
            {
                DeleteEventImageFile(item.ImageUrl);
                item.ImageUrl = null;
            }

            var uploadedImageUrl = await SaveEventImageAsync(item.Id, model.ImageFile);
            if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                DeleteEventImageFile(item.ImageUrl);
                item.ImageUrl = uploadedImageUrl;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Events.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _db.Events.Remove(item);
            await _db.SaveChangesAsync();

            DeleteEventImageFile(item.ImageUrl);

            return RedirectToAction(nameof(All));
        }

        private async Task<string?> SaveEventImageAsync(int eventId, IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaxFileBytes) return null;
            if (!PermittedImageContentTypes.Contains(file.ContentType)) return null;

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "images", "events", eventId.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            return $"/images/events/{eventId}/{fileName}";
        }

        private void DeleteEventImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/images/events/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var relative = imageUrl.TrimStart('/');
                var filePath = Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete event image file: {ImageUrl}", imageUrl);
            }
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var exists = await _db.UserEventParticipations.FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == id);
            if (exists == null)
            {
                _db.UserEventParticipations.Add(new Data.Models.UserEventParticipation { UserId = userId, EventId = id });
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var exists = await _db.UserEventParticipations.FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == id);
            if (exists != null)
            {
                _db.UserEventParticipations.Remove(exists);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private void ValidateEventImage(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return;
            }

            if (imageFile.Length > MaxFileBytes)
            {
                ModelState.AddModelError(nameof(EventFormViewModel.ImageFile), "Снимката трябва да е до 5 MB.");
            }

            if (!PermittedImageContentTypes.Contains(imageFile.ContentType))
            {
                ModelState.AddModelError(nameof(EventFormViewModel.ImageFile), "Позволени формати: JPG, PNG, GIF, WEBP.");
            }
        }

        private async Task<List<string>> GetAvailableEventTypesAsync()
        {
            return await _db.Events
                .AsNoTracking()
                .Where(e => e.Type != null && e.Type != string.Empty)
                .Select(e => e.Type!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        private static string NormalizeEventType(string currentType, string? customType)
        {
            return !string.IsNullOrWhiteSpace(customType)
                ? customType.Trim()
                : (currentType ?? string.Empty).Trim();
        }
    }
}