namespace EcoLoop.Models
{
    public class CartViewModel
    {
        public List<CartLineViewModel> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(x => x.LineTotal);
        public decimal TotalItems => Items.Sum(x => x.Quantity);
    }

    public class CartLineViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
