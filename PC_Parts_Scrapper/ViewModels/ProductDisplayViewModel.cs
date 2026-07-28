namespace PC_Parts_Scrapper.ViewModels
{
    public class ProductDisplayViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<StoreListingViewModel> Listings { get; set; } = new();
    }
}