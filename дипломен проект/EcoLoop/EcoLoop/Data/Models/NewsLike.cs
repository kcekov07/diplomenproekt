using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class NewsLike
    {
        public int Id { get; set; }

        public int NewsId { get; set; }
        public News News { get; set; } = null!;

        [Required, MaxLength(64)]
        public string VisitorKey { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
