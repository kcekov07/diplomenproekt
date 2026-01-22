using EcoLoop.Data;
using EcoLoop.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EcoLoop.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        private const string VisitorCookie = "ecoloop_vid";
        private const int CookieDays = 365;

        public CommentsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===== Helpers =====
        private string GetOrCreateVisitorKey()
        {
            if (Request.Cookies.TryGetValue(VisitorCookie, out var key) && !string.IsNullOrWhiteSpace(key))
                return key;

            var newKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // 32 chars
            Response.Cookies.Append(VisitorCookie, newKey, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(CookieDays)
            });

            return newKey;
        }

        private static string NewToken()
            => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); // 48 chars

        // ===== Create store review =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStoreReview(int storeId, string? visitorName, string text, int rating)
        {
            if (storeId <= 0) return BadRequest();
            if (string.IsNullOrWhiteSpace(text)) return RedirectToAction("Details", "Store", new { id = storeId });

            if (rating < 1) rating = 1;
            if (rating > 5) rating = 5;

            var storeExists = await _db.Stores.AnyAsync(s => s.Id == storeId);
            if (!storeExists) return NotFound();

            var visitorKey = GetOrCreateVisitorKey();

            var comment = new Comment
            {
                StoreId = storeId,
                VisitorName = string.IsNullOrWhiteSpace(visitorName) ? null : visitorName.Trim(),
                VisitorKey = visitorKey,
                EditToken = NewToken(),
                Text = text.Trim(),
                Rating = rating,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            // save edit token in cookie specific for that comment
            Response.Cookies.Append($"ecoloop_edit_{comment.Id}", comment.EditToken, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(CookieDays)
            });

            return RedirectToAction("Details", "Store", new { id = storeId });
        }

        // ===== Edit store review =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStoreReview(int id, string text, int rating)
        {
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id && c.StoreId != null);
            if (comment == null) return NotFound();

            // check ownership by edit token cookie
            if (!Request.Cookies.TryGetValue($"ecoloop_edit_{comment.Id}", out var token) || token != comment.EditToken)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(text)) return RedirectToAction("Details", "Store", new { id = comment.StoreId });

            if (rating < 1) rating = 1;
            if (rating > 5) rating = 5;

            comment.Text = text.Trim();
            comment.Rating = rating;
            comment.EditedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Store", new { id = comment.StoreId });
        }

        // ===== Delete store review =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStoreReview(int id)
        {
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id && c.StoreId != null);
            if (comment == null) return NotFound();

            if (!Request.Cookies.TryGetValue($"ecoloop_edit_{comment.Id}", out var token) || token != comment.EditToken)
                return Unauthorized();

            var storeId = comment.StoreId!.Value;

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            // best-effort remove cookie
            Response.Cookies.Delete($"ecoloop_edit_{comment.Id}");

            return RedirectToAction("Details", "Store", new { id = storeId });
        }

        // ===== Helpful toggle =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHelpful(int commentId)
        {
            var visitorKey = GetOrCreateVisitorKey();

            var exists = await _db.CommentHelpfuls
                .FirstOrDefaultAsync(h => h.CommentId == commentId && h.VisitorKey == visitorKey);

            if (exists == null)
            {
                _db.CommentHelpfuls.Add(new CommentHelpful
                {
                    CommentId = commentId,
                    VisitorKey = visitorKey
                });
            }
            else
            {
                _db.CommentHelpfuls.Remove(exists);
            }

            await _db.SaveChangesAsync();

            // redirect back
            var storeId = await _db.Comments.Where(c => c.Id == commentId).Select(c => c.StoreId).FirstOrDefaultAsync();
            if (storeId.HasValue) return RedirectToAction("Details", "Store", new { id = storeId.Value });

            return RedirectToAction("All", "Store");
        }
    }
}
