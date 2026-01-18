using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLoop.Data;

namespace EcoLoop.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var pendingStores = await _db.Stores.Where(s => !s.IsApproved).ToListAsync();
            ViewData["PendingStores"] = pendingStores;
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
    }
}