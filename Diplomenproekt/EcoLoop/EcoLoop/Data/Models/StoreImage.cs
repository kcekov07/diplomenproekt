using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class StoreImage
    {
        public int Id { get; set; }

        [Required]
        public int StoreId { get; set; }
        public Store? Store { get; set; }

        [Required, MaxLength(260)]
        public string FileName { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Url { get; set; } = null!;
    }
}