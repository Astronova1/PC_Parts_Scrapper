namespace PC_Parts_Scrapper.Models
{
    public class ScrapedItem
    {
        public int ScrapedItemId { get; set; }
        public Uri? Url { get; set; }
        public List<PriceHistory>? PriceHistory { get; private set; }  
        public int StoreId { get; set; }
        public Store? Store {  get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; } 

    }
}
