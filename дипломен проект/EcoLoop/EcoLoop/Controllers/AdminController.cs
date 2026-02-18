using EcoLoop.Data;
using EcoLoop.Data.Models;
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
            ViewData["PendingStores"] = await _db.Stores.Where(s => !s.IsApproved).ToListAsync();
            ViewData["Profiles"] = await _db.UserProfiles.OrderByDescending(x => x.CreatedOnUtc).ToListAsync();
            return View();
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
    }
}