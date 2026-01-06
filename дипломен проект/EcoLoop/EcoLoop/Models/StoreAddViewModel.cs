using System.ComponentModel.DataAnnotations;


namespace EcoLoop.Models
{
    public class StoreAddViewModel
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(200)]
        public string? ShortDescription { get; set; }

        [MaxLength(100)]
        public string? Description { get; set; }

        public string? Address { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public bool AcceptsOwnPackaging { get; set; }

        public bool IsProducer { get; set; }

        // Structured working hours
        [MaxLength(100)]
        public string? MonToFriHours { get; set; } // e.g. "09:00-18:00"

        [MaxLength(100)]
        public string? SatHours { get; set; }

        [MaxLength(100)]
        public string? SunHours { get; set; }

        public List<string>? Phones { get; set; } = new();

        public string? Website { get; set; }

        // Files
        public List<IFormFile>? Photos { get; set; }
    }
}