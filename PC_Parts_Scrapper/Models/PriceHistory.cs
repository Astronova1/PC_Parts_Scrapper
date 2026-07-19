namespace PC_Parts_Scrapper.Models
{
    public class PriceHistory
    {
        public int PriceHistoryId { get; set; }
        public int ScrapedItemId { get; set; }
        public ScrapedItem? ScrapedItem { get; set; }   
        public decimal Price {  get; set; }
        public DateTimeOffset ChecketAt { get; set; }

    }
}
