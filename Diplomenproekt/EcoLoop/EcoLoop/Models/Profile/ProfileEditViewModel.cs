using System.ComponentModel.DataAnnotations;

namespace EcoLoop.Models.Profile
{
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Потребителското име е задължително.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 80 символа.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Моля въведете валиден имейл.")]
        public string Email { get; set; } = string.Empty;

        public IFormFile? ProfileImage { get; set; }

        public string? CurrentProfileImageUrl { get; set; }
    }
}
