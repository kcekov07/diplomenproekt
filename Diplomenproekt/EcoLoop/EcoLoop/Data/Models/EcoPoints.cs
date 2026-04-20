namespace EcoLoop.Data.Models
{
    public class EcoPoints
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public int Points { get; set; }

        public int StoresAdded { get; set; }

        public int PackagesSaved { get; set; }
    }
}