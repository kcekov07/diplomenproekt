using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Models
{
    public class EventsIndexViewModel
    {
        public DateTime? DateFilter { get; set; }

        public string? TypeFilter { get; set; }

        public string? CityFilter { get; set; }

        public List<string> AvailableTypes { get; set; } = new();

        public List<string> AvailableCities { get; set; } = new();

        public List<EventViewModel> Events { get; set; } = new();
    }

    public class EventFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заглавието е задължително")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Display(Name = "Снимка")]
        public IFormFile? ImageFile { get; set; }

        public bool RemoveImage { get; set; }

        public string? ExistingImageUrl { get; set; }

        [Required(ErrorMessage = "Датата е задължителна")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Градът е задължителен")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;


        [StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CustomType { get; set; }

        public List<string> AvailableTypes { get; set; } = new();

        [Required(ErrorMessage = "Краткото описание е задължително")]
        [StringLength(400)]
        [Display(Name = "Кратко описание")]
        public string ShortDescription { get; set; } = string.Empty;
    }
}
