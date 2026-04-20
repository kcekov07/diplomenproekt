using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class CommentHelpful
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        [Required, MaxLength(64)]
        public string VisitorKey { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
