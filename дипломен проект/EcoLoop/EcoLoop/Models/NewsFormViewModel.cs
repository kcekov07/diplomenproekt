using System.ComponentModel.DataAnnotations;

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
        [Display(Name = "Основно изображение (URL)")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Категорията е задължителна")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Author { get; set; }
    }
}
