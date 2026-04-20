using System.Collections.Generic;

namespace EcoLoop.Models
{
    public class HomeViewModel
    {
        public List<StoreViewModel> NearbyStores { get; set; } = new();
        public List<NewsViewModel> TopNews { get; set; } = new();
        public List<EventViewModel> UpcomingEvents { get; set; } = new();

        public int StoresCount { get; set; }
        public int ReviewsCount { get; set; }
        public int EventsCount { get; set; }
    }
}