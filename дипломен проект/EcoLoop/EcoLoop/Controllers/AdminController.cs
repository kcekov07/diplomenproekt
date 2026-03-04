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
                }).ToList()
            };

            return View(model);
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

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}