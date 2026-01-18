using EcoLoop.Data;
using Microsoft.AspNetCore.Mvc;


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLoop.Data;
using System.Threading.Tasks;
using System.Linq;

namespace EcoLoop.Controllers
{
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MapController(ApplicationDbContext db)
        {
            _db = db;
        }

        // keep existing index (if used)
        public IActionResult Index()
        {
            return View("~/Views/Store/Map.cshtml");
        }

        // New route so the page is reachable at /map/store
        [HttpGet("map/store")]
        public IActionResult Store()
        {
            return View("~/Views/Store/Map.cshtml");
        }

        // JSON endpoint used by the client to load stores.
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
                    s.WorkingHours,
                    ImageUrl = s.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
                })
                .ToListAsync();

            return Json(stores);
        }
    }
}