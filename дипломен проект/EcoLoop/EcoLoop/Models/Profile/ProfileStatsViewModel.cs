namespace EcoLoop.Models.Profile
{
    public class ProfileStatsViewModel
    {
        public string Role { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int VisitedStores { get; set; }
        public int AddedObjects { get; set; }
        public int SavedPackages { get; set; }
        public int AchievementsCount { get; set; }
        public List<string> Badges { get; set; } = [];
    }
}
