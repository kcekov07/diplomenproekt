using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLoop.Data;
using EcoLoop.Models;

namespace EcoLoop.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public EventsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> All()
        {
            var items = await _db.Events
                .OrderBy(e => e.Date)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Date = e.Date,
                    City = e.City,
                    ShortDescription = e.ShortDescription
                })
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.Events.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }
    }
}