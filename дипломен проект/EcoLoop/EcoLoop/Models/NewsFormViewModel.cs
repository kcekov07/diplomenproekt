using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLoop.Models
{
    public class NewsFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заглавието е задължително")]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Добави съдържание")]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Основно изображение")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Качи изображение")]
        public IFormFile? UploadedImage { get; set; }

        [Display(Name = "Премахни текущото изображение")]
        public bool RemoveCurrentImage { get; set; }

        [Required(ErrorMessage = "Категорията е задължителна")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Author { get; set; }
    }
}