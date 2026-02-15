using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
namespace EcoLoop.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string VisitorCookie = "ecoloop_news_vid";

        private static readonly string[] FixedCategories =
        {
            "Еко бизнес", "Общество", "Съвети", "Законодателство", "Локални"
        };

        public NewsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> All(string? search, string? category)
        {
            var query = _db.News.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(n => n.Title.Contains(term) || (n.Content ?? string.Empty).Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(n => n.Category == category);
            }

            var topNews = await _db.News
                .AsNoTracking()
                .OrderByDescending(n => n.PublishedAt)
                .Take(3)
                .Select(n => new NewsListItemViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Category = n.Category ?? "Общо",
                    ImageUrl = n.ImageUrl,
                    PublishedAt = n.PublishedAt,
                    PreviewText = (n.Content ?? string.Empty).Length > 180 ? (n.Content ?? string.Empty).Substring(0, 180) + "..." : (n.Content ?? string.Empty),
                    LikesCount = _db.NewsLikes.Count(l => l.NewsId == n.Id),
                    CommentsCount = _db.Comments.Count(c => c.NewsId == n.Id)
                })
                .ToListAsync();

            var items = await query
                .OrderByDescending(n => n.PublishedAt)
                .Select(n => new NewsListItemViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Category = n.Category ?? "Общо",
                    ImageUrl = n.ImageUrl,
                    PublishedAt = n.PublishedAt,
                    PreviewText = (n.Content ?? string.Empty).Length > 220 ? (n.Content ?? string.Empty).Substring(0, 220) + "..." : (n.Content ?? string.Empty),
                    LikesCount = _db.NewsLikes.Count(l => l.NewsId == n.Id),
                    CommentsCount = _db.Comments.Count(c => c.NewsId == n.Id)
                })
                .ToListAsync();

            var model = new NewsIndexViewModel
            {
                Search = search,
                Category = category,
                Categories = FixedCategories.ToList(),
                TopNews = topNews,
                News = items
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.News.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            var visitorKey = GetOrCreateVisitorKey();

            var model = new NewsDetailsViewModel
            {
                Article = item,
                LikesCount = await _db.NewsLikes.CountAsync(l => l.NewsId == id),
                IsLikedByVisitor = await _db.NewsLikes.AnyAsync(l => l.NewsId == id && l.VisitorKey == visitorKey),
                Comments = await _db.Comments
                    .AsNoTracking()
                    .Where(c => c.NewsId == id)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync(),
                RecommendedNews = await _db.News
                    .AsNoTracking()
                    .Where(n => n.Id != id && n.Category == item.Category)
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(3)
                    .Select(n => new NewsListItemViewModel
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Category = n.Category ?? "Общо",
                        ImageUrl = n.ImageUrl,
                        PublishedAt = n.PublishedAt,
                        PreviewText = (n.Content ?? string.Empty).Length > 130 ? (n.Content ?? string.Empty).Substring(0, 130) + "..." : (n.Content ?? string.Empty),
                        LikesCount = _db.NewsLikes.Count(l => l.NewsId == n.Id),
                        CommentsCount = _db.Comments.Count(c => c.NewsId == n.Id)
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create() => View(new NewsFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = new News
            {
                Title = model.Title.Trim(),
                Content = model.Content.Trim(),
                Category = model.Category.Trim(),
                Author = string.IsNullOrWhiteSpace(model.Author) ? "EcoLoop Екип" : model.Author.Trim(),
                ImageUrl = model.ImageUrl,
                PublishedAt = DateTime.UtcNow
            };

            _db.News.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = entity.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _db.News.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            var model = new NewsFormViewModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content ?? string.Empty,
                Category = entity.Category ?? string.Empty,
                Author = entity.Author,
                ImageUrl = entity.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NewsFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = await _db.News.FindAsync(model.Id);
            if (entity == null)
            {
                return NotFound();
            }

            entity.Title = model.Title.Trim();
            entity.Content = model.Content.Trim();
            entity.Category = model.Category.Trim();
            entity.Author = string.IsNullOrWhiteSpace(model.Author) ? "EcoLoop Екип" : model.Author.Trim();
            entity.ImageUrl = model.ImageUrl;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = entity.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.News.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _db.News.Remove(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var visitorKey = GetOrCreateVisitorKey();
            var existing = await _db.NewsLikes.FirstOrDefaultAsync(l => l.NewsId == id && l.VisitorKey == visitorKey);

            if (existing == null)
            {
                _db.NewsLikes.Add(new NewsLike { NewsId = id, VisitorKey = visitorKey });
            }
            else
            {
                _db.NewsLikes.Remove(existing);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, string? visitorName, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            var exists = await _db.News.AnyAsync(n => n.Id == id);
            if (!exists)
            {
                return NotFound();
            }

            _db.Comments.Add(new Comment
            {
                NewsId = id,
                VisitorName = string.IsNullOrWhiteSpace(visitorName) ? null : visitorName.Trim(),
                VisitorKey = GetOrCreateVisitorKey(),
                EditToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
                Text = text.Trim(),
                Rating = 5,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        private string GetOrCreateVisitorKey()
        {
            if (Request.Cookies.TryGetValue(VisitorCookie, out var key) && !string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            var newKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            Response.Cookies.Append(VisitorCookie, newKey, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(365)
            });

            return newKey;
        }
    }
}