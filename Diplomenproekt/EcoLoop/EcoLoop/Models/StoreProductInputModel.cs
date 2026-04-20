using Microsoft.AspNetCore.Http;
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

        public IFormFile? ProductImage { get; set; }

        [StringLength(100)]
        public string? Unit { get; set; }

        [StringLength(200)]
        public string? Labels { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}