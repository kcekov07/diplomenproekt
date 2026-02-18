namespace EcoLoop.Data.Models
{
    public class UserVisitedStore
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        public Store Store { get; set; } = null!;
    }
}
