using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using EcoLoop.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLoop.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
            profile.Level = CalculateLevel(visitedStores, savedPackages, addedObjects);
            await _db.SaveChangesAsync();

            var badges = BuildBadges(profile.Role, visitedStores, savedPackages, addedObjects);

            return View(new ProfileStatsViewModel
            {
                Role = profile.Role,
                Level = profile.Level,
                VisitedStores = visitedStores,
                AddedObjects = addedObjects,
                SavedPackages = savedPackages,
                Badges = badges,
                AchievementsCount = badges.Count
            });
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

        private static string CalculateLevel(int visitedStores, int savedPackages, int addedObjects)
        {
            var score = visitedStores + savedPackages + (addedObjects * 2);
            if (score >= 30) return "Earth Guardian";
            if (score >= 12) return "Green Hero";
            return "Eco Explorer";
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
