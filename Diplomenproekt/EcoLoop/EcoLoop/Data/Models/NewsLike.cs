using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class NewsLike
    {
        public int Id { get; set; }

        public int NewsId { get; set; }
        public News News { get; set; } = null!;

        [Required, MaxLength(450)]
        public string UserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}