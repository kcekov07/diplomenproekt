using System.Collections.Generic;

namespace EcoLoop.Models
{
    public class MapStoreViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Category { get; set; }

        public string ShortDescription { get; set; }
        public string Address { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // ЕДИН URL за картата
        public string ImageUrl { get; set; }

        public bool AcceptsOwnPackaging { get; set; }
        public bool IsProducer { get; set; }
        public bool HasRefillStation { get; set; }
        public bool HasDelivery { get; set; }
        public double Rating { get; set; }

        public string WorkingHours { get; set; }
        public string EcoTags { get; set; }
        public string Certifications { get; set; }
    }
}
