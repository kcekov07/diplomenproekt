using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace EcoLoop.Models
{
    public class StoreEditViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(200)]
        public string? ShortDescription { get; set; }

        [MaxLength(4000)]
        public string? Description { get; set; }

        public string? Address { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public bool AcceptsOwnPackaging { get; set; }

        public bool IsProducer { get; set; }

        // Keep a single field for storage, but expose structured fields for editing
        public string? WorkingHours { get; set; }

        // Structured fields used in the form (Mon-Fri / Sat / Sun)
        [MaxLength(100)]
        public string? MonToFriHours { get; set; }

        [MaxLength(100)]
        public string? SatHours { get; set; }

        [MaxLength(100)]
        public string? SunHours { get; set; }

        public List<string>? Phones { get; set; } = new();

        public string? Website { get; set; }
        public string? EcoTags { get; set; }
        public string? Certifications { get; set; }
        public bool HasDelivery { get; set; }
        public bool HasRefillStation { get; set; }

        public string? Email { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
       

        // New photos to upload
        public List<IFormFile>? Photos { get; set; }

        // Existing images for display (Id and Url)
        public List<ExistingImageViewModel> ExistingImages { get; set; } = new();

        public class ExistingImageViewModel
        {
            public int Id { get; set; }
            public string Url { get; set; } = null!;
        }
    }
}