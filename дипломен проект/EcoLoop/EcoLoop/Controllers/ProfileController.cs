using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using EcoLoop.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace EcoLoop.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(ApplicationDbContext db, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
        {
            _db = db;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (profile == null) return NotFound();

            return View(new ProfileIndexViewModel
            {
                Username = profile.Username,
                Email = user.Email ?? string.Empty,
                Role = profile.Role,
                Level = profile.Level,
                ProfileImageUrl = profile.ProfileImageUrl
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (profile == null) return NotFound();

            return View(new ProfileEditViewModel
            {
                Username = profile.Username,
                Email = user.Email ?? string.Empty,
                CurrentProfileImageUrl = profile.ProfileImageUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (profile == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.CurrentProfileImageUrl = profile.ProfileImageUrl;
                return View(model);
            }

            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = model.Email.Trim();
                user.UserName = model.Email.Trim();
                var identityResult = await _userManager.UpdateAsync(user);
                if (!identityResult.Succeeded)
                {
                    foreach (var error in identityResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    model.CurrentProfileImageUrl = profile.ProfileImageUrl;
                    return View(model);
                }
            }

            profile.Username = model.Username.Trim();

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                if (model.ProfileImage.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.ProfileImage), "Снимката трябва да е до 5MB.");
                    model.CurrentProfileImageUrl = profile.ProfileImageUrl;
                    return View(model);
                }

                var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.ProfileImage), "Разрешени са само: .jpg, .jpeg, .png, .webp.");
                    model.CurrentProfileImageUrl = profile.ProfileImageUrl;
                    return View(model);
                }

                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRoot, "images", "profiles", user.Id); Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{user.Id}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = System.IO.File.Create(filePath))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                if (!string.IsNullOrWhiteSpace(profile.ProfileImageUrl))
                {
                    var oldPath = Path.Combine(webRoot, profile.ProfileImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                profile.ProfileImageUrl = $"/images/profiles/{user.Id}/{fileName}";
            }

            await _db.SaveChangesAsync();
            TempData["ProfileSuccess"] = "Профилът е обновен успешно.";

            return RedirectToAction(nameof(MyProfile));
        }

        public async Task<IActionResult> Statistics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (profile == null) return NotFound();

            var visitedStores = await _db.UserVisitedStores.CountAsync(x => x.UserId == user.Id);
            var addedObjects = await _db.Stores.CountAsync(x => x.CreatorId == user.Id);
            var savedPackages = profile.SavedPackages;

            profile.StoresVisited = visitedStores;
            profile.AddedObjects = addedObjects;
            var joinedEventsCount = await _db.UserEventParticipations.CountAsync(x => x.UserId == user.Id);
            profile.Level = CalculateLevel(profile.Role, visitedStores, savedPackages, addedObjects, joinedEventsCount);
            await _db.SaveChangesAsync();

            var badges = BuildBadges(profile.Role, visitedStores, savedPackages, addedObjects);

            var joinedEvents = await _db.UserEventParticipations
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new ProfileEventViewModel
                {
                    EventId = x.EventId,
                    Title = x.Event.Title,
                    Date = x.Event.Date,
                    City = x.Event.City,
                    Type = x.Event.Type
                })
                .ToListAsync();

            var favoriteStoresCount = await _db.UserFavoriteStores.CountAsync(x => x.UserId == user.Id);
            joinedEventsCount = joinedEvents.Count;
            var producerBonusPoints = CalculateProducerBonus(profile.Role, addedObjects);
            var totalPoints = CalculatePoints(profile.Role, visitedStores, savedPackages, addedObjects, joinedEventsCount);

            return View(new ProfileStatsViewModel
            {
                Role = profile.Role,
                Level = profile.Level,
                VisitedStores = visitedStores,
                AddedObjects = addedObjects,
                SavedPackages = savedPackages,
                JoinedEventsCount = joinedEventsCount,
                ProducerBonusPoints = producerBonusPoints,
                Badges = badges,
                AchievementsCount = badges.Count,
                FavoriteStoresCount = favoriteStoresCount,
                JoinedEvents = joinedEvents,
                TotalPoints = totalPoints,
                NextLevelPoints = CalculateNextLevelPoints(totalPoints),
                ProgressToNextLevelPercent = CalculateProgressToNextLevelPercent(totalPoints)
            });
        }

        public async Task<IActionResult> MyFavoriteStores()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var stores = await _db.UserFavoriteStores
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new StoreViewModel
                {
                    Id = x.Store.Id,
                    Name = x.Store.Name,
                    Category = x.Store.Category,
                    ShortDescription = x.Store.ShortDescription,
                    Rating = x.Store.Rating,
                    HasDelivery = x.Store.HasDelivery,
                    HasRefillStation = x.Store.HasRefillStation,
                    EcoTags = x.Store.EcoTags,
                    ImageUrl = x.Store.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
                })
                .ToListAsync();

            return View(stores);
        }

        public IActionResult LevelGuide()
        {
            return View();
        }

        [Authorize(Roles = "Producer,Moderator,Admin")]
        public async Task<IActionResult> MyStores()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var stores = await _db.Stores
                .Where(x => x.CreatorId == user.Id)
                .OrderByDescending(x => x.Id)
                .Select(x => new StoreViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Category = x.Category,
                    ShortDescription = x.ShortDescription,
                    Rating = x.Rating,
                    HasDelivery = x.HasDelivery,
                    HasRefillStation = x.HasRefillStation,
                    EcoTags = x.EcoTags,
                    ImageUrl = x.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
                })
                .ToListAsync();

            return View(stores);
        }

        private static int CalculatePoints(string role, int visitedStores, int savedPackages, int addedObjects, int joinedEvents)
           => visitedStores + savedPackages + (addedObjects * 3) + (joinedEvents * 2) + CalculateProducerBonus(role, addedObjects);

        private static int CalculateProducerBonus(string role, int addedObjects)
            => role == UserRoleType.Producer && addedObjects >= 3 ? 5 : 0;

        private static string CalculateLevel(string role, int visitedStores, int savedPackages, int addedObjects, int joinedEvents)
        {
            var score = CalculatePoints(role, visitedStores, savedPackages, addedObjects, joinedEvents);
            if (score >= 60) return "Earth Guardian";
            if (score >= 25) return "Green Hero";
            return "Eco Explorer";
        }
        private static int CalculateNextLevelPoints(int score)
        {
            if (score < 25) return 25;
            if (score < 60) return 60;
            return score;
        }

        private static int CalculateProgressToNextLevelPercent(int score)
        {
            if (score < 25)
            {
                return (int)Math.Clamp(Math.Round((score / 25.0) * 100), 0, 100);
            }

            if (score < 60)
            {
                return (int)Math.Clamp(Math.Round(((score - 25) / 35.0) * 100), 0, 100);
            }

            return 100;
        }


        private static List<string> BuildBadges(string role, int visitedStores, int savedPackages, int addedObjects)
        {
            var badges = new List<string>();
            if (visitedStores >= 1) badges.Add("🌍 Първи посетен магазин");
            if (visitedStores >= 10) badges.Add("🧭 Еко изследовател");
            if (savedPackages >= 20) badges.Add("💚 Спасител на опаковки");
            if (role == UserRoleType.Producer && addedObjects >= 1) badges.Add("♻️ Еко производител");
            if (addedObjects >= 5) badges.Add("⭐ Създател на зелена общност");
            return badges;
        }
    }
}