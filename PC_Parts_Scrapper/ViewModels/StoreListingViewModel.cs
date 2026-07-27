namespace PC_Parts_Scrapper.Views.ViewModels
{
    public class StoreListingViewModel
    {
        public string StoreName { get; set; } = string.Empty;
        public Uri? Url { get; set; }
        public decimal LatestPrice { get; set; }
    }
}
