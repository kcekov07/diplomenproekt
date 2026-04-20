using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }
        
        public int? NewsId { get; set; }
        public News? News { get; set; }

        public int? StoreId { get; set; }
        public Store? Store { get; set; }

        [MaxLength(60)]
        public string? VisitorName { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }


        [Required, MaxLength(64)]
        public string VisitorKey { get; set; } = null!;

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
