using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Models;
using System.Security.Cryptography.X509Certificates;

namespace PC_Parts_Scrapper.Data
{       //addDbContext registers the DbContext as scoped Lifetime i.e  services are created once per client request (connection).
    public class PcPartsContext : DbContext  
    {   
        public PcPartsContext(DbContextOptions<PcPartsContext> options) : base(options)
        { 
        }

        public DbSet<Store> Stores { get; set; }
        public DbSet<Product> Products { get; set; } 
        public DbSet<ScrapedItem> ScrapedItems { get; set; }   
        public DbSet<PriceHistory> PriceHistory { get; set; }
    }
}
