

using EcoLoop.Data;
using EcoLoop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EcoLoop.Controllers
{
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MapController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stores = await _context.Stores
                .Where(s => s.IsApproved && s.Latitude != 0 && s.Longitude != 0)
                .Select(s => new MapStoreViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    ShortDescription = s.ShortDescription,
                    Address = s.Address,

                    Latitude = (double)(s.Latitude ?? 0),
                    Longitude = (double)(s.Longitude ?? 0),
                    Rating = (double)s.Rating,

                    AcceptsOwnPackaging = s.AcceptsOwnPackaging,
                    WorkingHours = s.WorkingHours,

                    ImageUrl = s.Images
        .Select(i => i.Url)
        .FirstOrDefault()

                })
                .ToListAsync();

            return View(new MapPageViewModel
            {
                Stores = stores
            });
        }

    }
}
