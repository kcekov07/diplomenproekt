using EcoLoop.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace EcoLoop.Models
{
    public class AdminDashboardViewModel
    {
        public List<Store> PendingStores { get; set; } = new();
        public List<Store> AllStores { get; set; } = new();
        public List<UserProfileAdminItemViewModel> Users { get; set; } = new();
        public List<CommentAdminItemViewModel> RecentComments { get; set; } = new();
    }

    public class UserProfileAdminItemViewModel
    {
        public required UserProfile Profile { get; set; }
        public IdentityUser? IdentityUser { get; set; }
        public bool IsLocked { get; set; }
    }

    public class CommentAdminItemViewModel
    {
        public required Comment Comment { get; set; }
        public string TargetTitle { get; set; } = string.Empty;
    }
}
