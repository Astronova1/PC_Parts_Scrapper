namespace PC_Parts_Scrapper.ViewModels
{
    public class AlertDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TargetPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActiveAt { get; set; }
    }
}
