using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime PublishedAt { get; set; }

        public string? Author { get; set; }

        public string? Category { get; set; }
    }
}