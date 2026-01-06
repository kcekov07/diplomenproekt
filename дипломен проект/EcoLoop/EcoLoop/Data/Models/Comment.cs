using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLoop.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int? NewsId { get; set; }

        [ForeignKey(nameof(NewsId))]
        public News? News { get; set; }

        public string? UserId { get; set; }

        public string? Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}