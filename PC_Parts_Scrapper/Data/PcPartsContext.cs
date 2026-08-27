using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Models;
using System.Security.Cryptography.X509Certificates;

namespace PC_Parts_Scrapper.Data
{       //addDbContext registers the DbContext as scoped Lifetime i.e  services are created once per client request (connection).
    public class PcPartsContext : IdentityDbContext<ApplicationUser>
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScrapedItem>(Entity => {
                    Entity.HasKey(e => e.ScrapedItemId);            //make scrapedItemId the primary key

                Entity.Property(e =>e.ScrapedItemId)
                                   .UseIdentityByDefaultColumn();       //add key to auto generate
                }

            );

            base.OnModelCreating(modelBuilder);
        }
        public PcPartsContext(DbContextOptions<PcPartsContext> options) : base(options)
        { 
        }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Product> Products { get; set; } 
        public DbSet<ScrapedItem> ScrapedItems { get; set; }   
        public DbSet<PriceHistory> PriceHistory { get; set; }
        public DbSet<Category> Categories { get; set;}
        public DbSet<PriceAlert> PriceAlerts { get; set; }
        public DbSet<Notification> Notifications { get; set; }

    }
}
