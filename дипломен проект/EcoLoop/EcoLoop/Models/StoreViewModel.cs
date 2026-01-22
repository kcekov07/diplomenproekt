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
        public string? EcoTags { get; set; }
        public string? Certifications { get; set; }
        public bool HasDelivery { get; set; }
        public bool HasRefillStation { get; set; }

        public string? Email { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        

    }
}