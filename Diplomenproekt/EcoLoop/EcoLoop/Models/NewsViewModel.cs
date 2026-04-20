namespace EcoLoop.Models
{
    public class NewsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? ShortContent { get; set; }

        public string? Category { get; set; }

        public DateTime PublishedAt { get; set; }
    }
}