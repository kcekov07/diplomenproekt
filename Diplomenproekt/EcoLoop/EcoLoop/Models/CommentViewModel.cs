namespace EcoLoop.Models
{
    public class CommentViewModel
    {
        public int Id { get; set; }

        public string? UserName { get; set; }

        public string? Text { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}