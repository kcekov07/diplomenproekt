using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Models
{
    public class StoreProductInputModel
    {
        [Required]
        public int StoreId { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(160)]
        public string Name { get; set; } = string.Empty;

        [StringLength(600)]
        public string? Description { get; set; }

        [Range(0.01, 99999, ErrorMessage = "Цената трябва да е над 0.")]
        public decimal Price { get; set; }

        [StringLength(300)]
        [Url(ErrorMessage = "Добави валиден URL за изображение.")]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Unit { get; set; }

        [StringLength(200)]
        public string? Labels { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}