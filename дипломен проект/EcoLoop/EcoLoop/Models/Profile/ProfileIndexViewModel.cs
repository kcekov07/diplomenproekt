namespace EcoLoop.Models.Profile
{
    public class ProfileIndexViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }
}
