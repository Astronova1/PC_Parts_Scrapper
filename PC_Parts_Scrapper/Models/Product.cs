namespace PC_Parts_Scrapper.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public String Name { get; set; } = String.Empty;
        public ICollection<ScrapedItem> ScrapedItems { get; set; } = new List<ScrapedItem>();
        public Category? Category { get; set; }
    }
}
