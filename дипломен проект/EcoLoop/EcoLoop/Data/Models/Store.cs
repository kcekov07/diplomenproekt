using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class Store
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Category { get; set; }

        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        public string? Address { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public bool AcceptsOwnPackaging { get; set; }

        public bool IsProducer { get; set; }

       

        public string? Website { get; set; }

        public decimal Rating { get; set; }

        public bool Approved { get; set; } = false;

        // New
        public string? WorkingHours { get; set; }

        // Navigation
        public List<StoreImage> Images { get; set; } = new();
        public List<StorePhone> Phones { get; set; } = new();
    }
}