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

        [Range(typeof(decimal), "0.1", "99")]
        public decimal Quantity { get; set; } = 1m;

        public DateTime AddedOn { get; set; } = DateTime.UtcNow;
    }
}
