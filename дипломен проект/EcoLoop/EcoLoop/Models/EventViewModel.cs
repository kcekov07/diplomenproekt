namespace EcoLoop.Models
{
    public class EventViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public DateTime Date { get; set; }

        public string? City { get; set; }
        public string? Type { get; set; }

        public string? ImageUrl { get; set; }

        public string? ShortDescription { get; set; }
    }
}