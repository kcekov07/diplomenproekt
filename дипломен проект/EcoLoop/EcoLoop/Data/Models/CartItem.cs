using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = null!;

        public int StoreProductId { get; set; }
        public StoreProduct StoreProduct { get; set; } = null!;

        [Range(1, 99)]
        public int Quantity { get; set; } = 1;

        public DateTime AddedOn { get; set; } = DateTime.UtcNow;
    }
}
