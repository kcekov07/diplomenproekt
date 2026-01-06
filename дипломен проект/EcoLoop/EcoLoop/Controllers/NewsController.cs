using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLoop.Data;
using EcoLoop.Models;

namespace EcoLoop.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public NewsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> All()
        {
            var items = await _db.News
                .OrderByDescending(n => n.PublishedAt)
                .Select(n => new NewsViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortContent = n.Content,
                    Category = n.Category,
                    PublishedAt = n.PublishedAt
                })
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.News.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }
    }
}