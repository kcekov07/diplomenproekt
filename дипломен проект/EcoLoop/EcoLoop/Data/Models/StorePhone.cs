using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class StorePhone
    {
        public int Id { get; set; }

        [Required]
        public int StoreId { get; set; }
        public Store? Store { get; set; }

        [Required, MaxLength(50)]
        public string PhoneNumber { get; set; } = null!;
    }
}