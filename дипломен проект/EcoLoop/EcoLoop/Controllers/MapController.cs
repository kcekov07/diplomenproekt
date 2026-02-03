using EcoLoop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EcoLoop.Controllers
{
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MapController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View("~/Views/Store/Map.cshtml");
        }

        [HttpGet("map/store")]
        public IActionResult Store()
        {
            return View("~/Views/Store/Map.cshtml");
        }

        [HttpGet("/Store/GetStores")]
        public async Task<IActionResult> GetStores()
        {
            var stores = await _db.Stores
                .Where(s => s.Latitude != null && s.Longitude != null)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Category,
                    s.ShortDescription,
                    s.Address,
                    s.Latitude,
                    s.Longitude,
                    s.Rating,
                    s.AcceptsOwnPackaging,
                    s.HasDelivery,
                    s.HasRefillStation,
                    s.EcoTags, // comma-separated string "Zero-waste,Bio,Local"
                    s.WorkingHours,
                    ImageUrl = s.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
                })
                .ToListAsync();

            return Json(stores);
        }
    }
}
