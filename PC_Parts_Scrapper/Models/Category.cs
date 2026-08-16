namespace PC_Parts_Scrapper.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public ICollection<Product>? Products { get; set; }
            
    }
}
