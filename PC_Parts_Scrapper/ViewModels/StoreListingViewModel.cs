namespace PC_Parts_Scrapper.ViewModels
{
    public class StoreListingViewModel
    {
        public string StoreName { get; set; } = string.Empty;
        public Uri? Url { get; set; }
        public decimal LatestPrice { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public DateTimeOffset? CheckedAt { get; set; }
    }
}
