using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? ShortDescription { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime Date { get; set; }

        public string? City { get; set; }

        public string? Type { get; set; }
    }
}