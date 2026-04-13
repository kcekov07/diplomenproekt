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

        public AdminAnalyticsViewModel Analytics { get; set; } = new();
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

    public class AdminAnalyticsViewModel
    {
        public decimal PotentialRevenue { get; set; }
        public decimal PotentialRevenueLast30Days { get; set; }
        public int StoreViews { get; set; }
        public int StoreViewsLast30Days { get; set; }
        public int FavoriteActions { get; set; }
        public int FavoriteActionsLast30Days { get; set; }
        public int CartActions { get; set; }
        public int CartActionsLast30Days { get; set; }
        public decimal ViewToFavoriteRate { get; set; }
        public decimal ViewToCartRate { get; set; }
        public List<StorePerformanceItemViewModel> TopStorePerformance { get; set; } = new();
    }

    public class StorePerformanceItemViewModel
    {
        public required Store Store { get; set; }
        public int Views { get; set; }
        public int Favorites { get; set; }
        public int Carts { get; set; }
        public decimal PotentialRevenue { get; set; }
    }
}