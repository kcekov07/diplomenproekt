namespace EcoLoop.Models
{
    public class NewsIndexViewModel
    {
        public string? Search { get; set; }

        public string? Category { get; set; }

        public List<string> Categories { get; set; } = new();

        public List<NewsListItemViewModel> TopNews { get; set; } = new();

        public List<NewsListItemViewModel> News { get; set; } = new();
    }

    public class NewsListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PreviewText { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
    }
}
