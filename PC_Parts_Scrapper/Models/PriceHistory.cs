namespace PC_Parts_Scrapper.Models
{
    public class PriceHistory
    {
        public int ScrapedItemId { get; set; }
        public ScrapedItem? ScrapedItem { get; set; }   
        public int PriceHistoryId {  get; set; }
        public decimal Price {  get; set; }
        public DateTimeOffset Date { get; set; }

    }
}
