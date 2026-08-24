using System.ComponentModel.DataAnnotations.Schema;

namespace PC_Parts_Scrapper.Models
{
    public class PriceAlert
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal TargetPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
