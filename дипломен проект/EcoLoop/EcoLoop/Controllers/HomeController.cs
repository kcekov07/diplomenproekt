using EcoLoop.Data;
using EcoLoop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLoop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }
        
        public async Task<IActionResult> Index()
        {
            // Top stores by average review rating (4)
            var topStores = await _db.Stores
                .Where(s => s.IsApproved)
                .Select(s => new
                {
                    Store = s,
                    AverageRating = s.Comments.Any()
                        ? s.Comments.Average(c => (decimal)c.Rating)
                        : s.Rating
                })
                .OrderByDescending(x => x.AverageRating)
                .Take(4)
                .Select(s => new StoreViewModel
                {
                    Id = s.Store.Id,
                    Name = s.Store.Name,
                    Category = s.Store.Category,
                    ShortDescription = s.Store.ShortDescription,
                    Latitude = s.Store.Latitude,
                    Longitude = s.Store.Longitude,
                    Rating = s.AverageRating,
                    ImageUrl = s.Store.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
                })
                .ToListAsync();

            // Top news (3)
            var latestNews = await _db.News
                .OrderByDescending(n => n.PublishedAt)
                .Take(3)
                .Select(n => new NewsViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortContent = n.Content,
                    Category = n.Category,
                    PublishedAt = n.PublishedAt
                })
                .ToListAsync();

            // Upcoming events (3)
            var upcoming = await _db.Events
                .Where(e => e.Date >= DateTime.UtcNow)
                .OrderBy(e => e.Date)
                .Take(3)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Date = e.Date,
                    City = e.City,
                    ShortDescription = e.ShortDescription
                })
                .ToListAsync();

            var vm = new HomeViewModel
            {
                NearbyStores = topStores,
                TopNews = latestNews,
                UpcomingEvents = upcoming,
                StoresCount = await _db.Stores.CountAsync(),
                ReviewsCount = await _db.Comments.CountAsync(),
                EventsCount = await _db.Events.CountAsync()
            };

            return View(vm);
        }

        public IActionResult Privacy() => View();
    }
}