using EcoLoop.Data.Models;

namespace EcoLoop.Models
{
    public class NewsDetailsViewModel
    {
        public News Article { get; set; } = null!;

        public List<Comment> Comments { get; set; } = new();

        public List<NewsListItemViewModel> RecommendedNews { get; set; } = new();

        public int LikesCount { get; set; }

        public bool IsLikedByVisitor { get; set; }
    }
}
