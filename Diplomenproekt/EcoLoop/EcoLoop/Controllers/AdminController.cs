using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLoop.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var pendingStores = await _db.Stores
                .Where(s => !s.IsApproved)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var allStores = await _db.Stores
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var profiles = await _db.UserProfiles
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToListAsync();

            var usersById = await _userManager.Users
                .Where(x => profiles.Select(p => p.UserId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            var comments = await _db.Comments
                .AsNoTracking()
                .Include(c => c.Store)
                .Include(c => c.News)
                .OrderByDescending(c => c.CreatedAt)
                .Take(30)
                .ToListAsync();

            var analytics = await BuildAnalyticsViewModelAsync(allStores);

            var model = new AdminDashboardViewModel
            {
                PendingStores = pendingStores,
                AllStores = allStores,
                Users = profiles.Select(profile =>
                {
                    usersById.TryGetValue(profile.UserId, out var identityUser);
                    return new UserProfileAdminItemViewModel
                    {
                        Profile = profile,
                        IdentityUser = identityUser,
                        IsLocked = identityUser?.LockoutEnd.HasValue == true && identityUser.LockoutEnd.Value > DateTimeOffset.UtcNow
                    };
                }).ToList(),
                RecentComments = comments.Select(comment => new CommentAdminItemViewModel
                {
                    Comment = comment,
                    TargetTitle = comment.Store?.Name ?? comment.News?.Title ?? "Без връзка"
                }).ToList(),
                Analytics = analytics
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AnalyticsSnapshot()
        {
            var allStores = await _db.Stores
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var analytics = await BuildAnalyticsViewModelAsync(allStores);

            return Json(new
            {
                potentialRevenue = analytics.PotentialRevenue,
                potentialRevenueLast30Days = analytics.PotentialRevenueLast30Days,
                storeViews = analytics.StoreViews,
                storeViewsLast30Days = analytics.StoreViewsLast30Days,
                favoriteActions = analytics.FavoriteActions,
                favoriteActionsLast30Days = analytics.FavoriteActionsLast30Days,
                cartActions = analytics.CartActions,
                cartActionsLast30Days = analytics.CartActionsLast30Days,
                viewToFavoriteRate = analytics.ViewToFavoriteRate,
                viewToCartRate = analytics.ViewToCartRate,
                topStorePerformance = analytics.TopStorePerformance.Select(x => new
                {
                    name = x.Store.Name,
                    category = x.Store.Category,
                    views = x.Views,
                    favorites = x.Favorites,
                    carts = x.Carts,
                    potentialRevenue = x.PotentialRevenue
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStore(int id)
        {
            var store = await _db.Stores.FindAsync(id);
            if (store == null) return NotFound();
            store.IsApproved = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStore(int id)
        {
            var store = await _db.Stores.FindAsync(id);
            if (store == null) return NotFound();
            _db.Stores.Remove(store);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToModerator(int profileId)
        {
            var profile = await _db.UserProfiles.FindAsync(profileId);
            if (profile == null) return NotFound();

            var user = await _userManager.FindByIdAsync(profile.UserId);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, UserRoleType.Moderator))
            {
                await _userManager.AddToRoleAsync(user, UserRoleType.Moderator);
            }

            profile.Role = UserRoleType.Moderator;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int profileId)
        {
            var profile = await _db.UserProfiles.FindAsync(profileId);
            if (profile == null) return NotFound();

            var user = await _userManager.FindByIdAsync(profile.UserId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            _db.UserProfiles.Remove(profile);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserBlock(int profileId)
        {
            var profile = await _db.UserProfiles.FindAsync(profileId);
            if (profile == null) return NotFound();

            var user = await _userManager.FindByIdAsync(profile.UserId);
            if (user == null) return NotFound();

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            user.LockoutEnabled = true;
            user.LockoutEnd = isLocked ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow.AddYears(100);

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return NotFound();
            var storeId = comment.StoreId;
            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            if (storeId.HasValue)
            {
                await RecalculateStoreRatingAsync(storeId.Value);
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task RecalculateStoreRatingAsync(int storeId)
        {
            var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
            if (store == null)
            {
                return;
            }

            var averageRating = await _db.Comments
                .Where(c => c.StoreId == storeId)
                .Select(c => (decimal?)c.Rating)
                .AverageAsync() ?? 0m;

            store.Rating = Math.Round(averageRating, 1, MidpointRounding.AwayFromZero);
            await _db.SaveChangesAsync();
        }
        private async Task<AdminAnalyticsViewModel> BuildAnalyticsViewModelAsync(List<Store> allStores)
        {
            var utcNow = DateTime.UtcNow;
            var monthStart = utcNow.AddDays(-30);

            var storeViewsByStore = await _db.UserVisitedStores
                .AsNoTracking()
                .GroupBy(x => x.StoreId)
                .Select(g => new { StoreId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoreId, x => x.Count);

            var storeFavoritesByStore = await _db.UserFavoriteStores
                .AsNoTracking()
                .GroupBy(x => x.StoreId)
                .Select(g => new { StoreId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoreId, x => x.Count);

            var cartByStore = await _db.CartItems
                .AsNoTracking()
                .Include(ci => ci.StoreProduct)
                .GroupBy(ci => ci.StoreProduct.StoreId)
                .Select(g => new
                {
                    StoreId = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(ci => ci.Quantity * ci.StoreProduct.Price)
                })
                .ToListAsync();

            var cartByStoreCount = cartByStore.ToDictionary(x => x.StoreId, x => x.Count);
            var cartByStoreRevenue = cartByStore.ToDictionary(x => x.StoreId, x => x.Revenue);

            var totalViews = storeViewsByStore.Values.Sum();
            var totalFavorites = storeFavoritesByStore.Values.Sum();
            var totalCartActions = cartByStoreCount.Values.Sum();
            var totalPotentialRevenue = cartByStoreRevenue.Values.Sum();

            var viewsLast30Days = await _db.UserVisitedStores.CountAsync(x => x.CreatedOnUtc >= monthStart);
            var favoritesLast30Days = await _db.UserFavoriteStores.CountAsync(x => x.CreatedOnUtc >= monthStart);
            var cartsLast30Days = await _db.CartItems.CountAsync(x => x.AddedOn >= monthStart);
            var potentialRevenueLast30Days = await _db.CartItems
                .AsNoTracking()
                .Where(x => x.AddedOn >= monthStart)
                .Include(x => x.StoreProduct)
                .SumAsync(x => x.Quantity * x.StoreProduct.Price);

            var topStorePerformance = allStores
                .Select(store => new StorePerformanceItemViewModel
                {
                    Store = store,
                    Views = storeViewsByStore.GetValueOrDefault(store.Id, 0),
                    Favorites = storeFavoritesByStore.GetValueOrDefault(store.Id, 0),
                    Carts = cartByStoreCount.GetValueOrDefault(store.Id, 0),
                    PotentialRevenue = cartByStoreRevenue.GetValueOrDefault(store.Id, 0m)
                })
                .OrderByDescending(x => x.PotentialRevenue)
                .ThenByDescending(x => x.Views)
                .Take(8)
                .ToList();

            if (topStorePerformance.Count == 0 && allStores.Count > 0)
            {
                topStorePerformance = allStores
                    .Take(8)
                    .Select(store => new StorePerformanceItemViewModel
                    {
                        Store = store,
                        Views = 0,
                        Favorites = 0,
                        Carts = 0,
                        PotentialRevenue = 0m
                    })
                    .ToList();
            }

            return new AdminAnalyticsViewModel
            {
                PotentialRevenue = totalPotentialRevenue,
                PotentialRevenueLast30Days = potentialRevenueLast30Days,
                StoreViews = totalViews,
                StoreViewsLast30Days = viewsLast30Days,
                FavoriteActions = totalFavorites,
                FavoriteActionsLast30Days = favoritesLast30Days,
                CartActions = totalCartActions,
                CartActionsLast30Days = cartsLast30Days,
                ViewToFavoriteRate = totalViews == 0 ? 0 : (decimal)totalFavorites / totalViews,
                ViewToCartRate = totalViews == 0 ? 0 : (decimal)totalCartActions / totalViews,
                TopStorePerformance = topStorePerformance
            };
        }
    }
}