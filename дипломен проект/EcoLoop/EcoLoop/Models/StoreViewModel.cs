namespace EcoLoop.Models
{
    public class StoreViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Category { get; set; }

        public string? ShortDescription { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public decimal Rating { get; set; }
        public string? ImageUrl { get; set; }
    }
}