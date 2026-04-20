using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Data.Models
{
    public class UserProfile
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = null!;

        [Required, MaxLength(80)]
        public string Username { get; set; } = null!;

        [MaxLength(512)]
        public string? ProfileImageUrl { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = UserRoleType.User;

        [Required, MaxLength(40)]
        public string Level { get; set; } = "Eco Explorer";

        public int SavedPackages { get; set; }
        public int StoresVisited { get; set; }
        public int AddedObjects { get; set; }

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
