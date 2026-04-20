using EcoLoop.Data;
using EcoLoop.Data.Models;
using EcoLoop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace EcoLoop.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private const long MaxFileBytes = 5 * 1024 * 1024;

        private static readonly string[] FixedCategories =
        {
            "Еко бизнес", "Общество", "Съвети", "Законодателство", "Локални"
        };

        public NewsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }
        public async Task<IActionResult> All(string? search, string? category)
        {
            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var query = _db.News.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var pattern = $"%{normalizedSearch}%";
                query = query.Where(n =>
                    EF.Functions.Like(n.Title, pattern) ||
                    EF.Functions.Like(n.Content ?? string.Empty, pattern) ||
                    EF.Functions.Like(n.Author ?? string.Empty, pattern) ||
                    EF.Functions.Like(n.Category ?? string.Empty, pattern));
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
                Search = normalizedSearch,
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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var model = new NewsDetailsViewModel
            {
                Article = item,
                LikesCount = await _db.NewsLikes.CountAsync(l => l.NewsId == id),
                IsLikedByUser = !string.IsNullOrWhiteSpace(currentUserId) && await _db.NewsLikes.AnyAsync(l => l.NewsId == id && l.UserId == currentUserId),
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
            }; ViewBag.EditableNewsCommentIds = string.IsNullOrWhiteSpace(currentUserId)
                ? new HashSet<int>()
                : model.Comments.Where(c => c.UserId == currentUserId).Select(c => c.Id).ToHashSet();

            if (!model.RecommendedNews.Any())
            {
                model.RecommendedNews = await _db.News
                    .AsNoTracking()
                    .Where(n => n.Id != id)
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
                    .ToListAsync();
            }


            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public IActionResult Create() => View(new NewsFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Moderator")]
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
                PublishedAt = DateTime.UtcNow
            };

            _db.News.Add(entity);
            await _db.SaveChangesAsync();
            entity.ImageUrl = await SaveNewsImageAsync(entity.Id, model.UploadedImage);
            await _db.SaveChangesAsync();


            return RedirectToAction(nameof(Details), new { id = entity.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
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
        [Authorize(Roles = "Admin,Moderator")]
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
            if (model.RemoveCurrentImage)
            {
                DeleteNewsImage(entity.ImageUrl);
                entity.ImageUrl = null;
            }

            if (model.UploadedImage != null)
            {
                DeleteNewsImage(entity.ImageUrl);
                entity.ImageUrl = await SaveNewsImageAsync(entity.Id, model.UploadedImage);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = entity.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.News.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            DeleteNewsImage(entity.ImageUrl);
            _db.News.Remove(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(All));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var existing = await _db.NewsLikes.FirstOrDefaultAsync(l => l.NewsId == id && l.UserId == currentUserId);

            if (existing == null)
            {
                _db.NewsLikes.Add(new NewsLike { NewsId = id, UserId = currentUserId });
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
        [Authorize]
        public async Task<IActionResult> AddComment(int id, string? visitorName, string text)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }
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
                UserId = userId,
                VisitorName = string.IsNullOrWhiteSpace(visitorName) ? User.Identity?.Name : visitorName.Trim(),
                VisitorKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                EditToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
                Text = text.Trim(),
                Rating = 5,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditComment(int commentId, string text)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return RedirectToAction(nameof(All));
            }

            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.NewsId != null);
            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserId != currentUserId)
            {
                return Forbid();
            }

            comment.Text = text.Trim();
            comment.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = comment.NewsId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.NewsId != null);
            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserId != currentUserId)
            {
                return Forbid();
            }

            var newsId = comment.NewsId;
            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = newsId });
        }

        private async Task<string?> SaveNewsImageAsync(int newsId, IFormFile? uploadedImage)
        {
            if (uploadedImage == null || uploadedImage.Length == 0 || uploadedImage.Length > MaxFileBytes)
            {
                return null;
            }

            var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!permitted.Contains(uploadedImage.ContentType))
            {
                return null;
            }

            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "images", "news", newsId.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(uploadedImage.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await uploadedImage.CopyToAsync(stream);

            return $"/images/news/{newsId}/{fileName}";
        }

        private void DeleteNewsImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/images/news/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, relative);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}