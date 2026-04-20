using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class StoreProduct
    {
        public int Id { get; set; }

        public int StoreId { get; set; }
        public Store Store { get; set; } = null!;

        [Required, MaxLength(160)]
        public string Name { get; set; } = null!;

        [MaxLength(600)]
        public string? Description { get; set; }

        [Range(0, 99999)]
        public decimal Price { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        [MaxLength(100)]
        public string? Unit { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(200)]
        public string? Labels { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}