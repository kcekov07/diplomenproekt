namespace EcoLoop.Models.Profile
{
    public class ProfileEventViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? City { get; set; }
        public string? Type { get; set; }
    }
}
