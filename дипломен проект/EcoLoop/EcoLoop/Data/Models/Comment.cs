using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }

        // Existing for News
        public int? NewsId { get; set; }
        public News? News { get; set; }

        // NEW for Store reviews
        public int? StoreId { get; set; }
        public Store? Store { get; set; }

        // No profiles now
        [MaxLength(60)]
        public string? VisitorName { get; set; }

        // Internal key for “ownership” without profiles
        [Required, MaxLength(64)]
        public string VisitorKey { get; set; } = null!;

        // Token to allow edit/delete (kept secret in cookie)
        [Required, MaxLength(64)]
        public string EditToken { get; set; } = null!;

        [Required, MaxLength(2000)]
        public string Text { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
    }
}
