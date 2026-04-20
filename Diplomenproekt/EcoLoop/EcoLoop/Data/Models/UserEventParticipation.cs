namespace EcoLoop.Data.Models
{
    public class UserEventParticipation
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        public Event Event { get; set; } = null!;
    }
}
