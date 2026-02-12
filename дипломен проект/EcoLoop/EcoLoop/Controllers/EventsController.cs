using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLoop.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public EventsController(ApplicationDbContext db)
        {
            _db = db;
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
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = new Event
            {
                Title = model.Title,
                ImageUrl = model.ImageUrl,
                Date = model.Date,
                City = model.City,
                Type = model.Type,
                ShortDescription = model.ShortDescription
            };

            _db.Events.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Events.FindAsync(id);
            if (item == null) return NotFound();

            var model = new EventFormViewModel
            {
                Id = item.Id,
                Title = item.Title,
                ImageUrl = item.ImageUrl,
                Date = item.Date,
                City = item.City ?? string.Empty,
                Type = item.Type ?? string.Empty,
                ShortDescription = item.ShortDescription ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var item = await _db.Events.FindAsync(model.Id);
            if (item == null) return NotFound();

            item.Title = model.Title;
            item.ImageUrl = model.ImageUrl;
            item.Date = model.Date;
            item.City = model.City;
            item.Type = model.Type;
            item.ShortDescription = model.ShortDescription;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
    }
}