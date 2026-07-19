namespace PC_Parts_Scrapper.Models
{
    // This class will contain properties about the store
    public class Store
    {
        public int StoreId { get; set; }
        public string Name { get; set; } = string.Empty;
        public required Uri URL { get; set; }
    }
}