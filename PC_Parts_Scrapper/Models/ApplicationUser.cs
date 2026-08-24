using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;

namespace PC_Parts_Scrapper.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<PriceAlert>? PriceAlerts { get; set; }
    }
}
