namespace PC_Parts_Scrapper.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public String Name { get; set; } = String.Empty;
        public ICollection<ScrapedItem> ScrapedItems { get; set; } = new List<ScrapedItem>();
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<PriceAlert> PriceAlerts { get; set; } = new List<PriceAlert>();
    }
}
