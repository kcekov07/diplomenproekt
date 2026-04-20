using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class CommentLike
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = null!;
    }
}
