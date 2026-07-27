using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
namespace PC_Parts_Scrapper.Controllers
{
    public class ProductController : Controller
    {
        private readonly PcPartsContext _context;  //implement injecting context in the controller constructor to access the database
        public ProductController(PcPartsContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> getProducts()
        {

            var query = await (
                from p in _context.Products
                where p.Name != null
                select new
                {
                    p.ProductId,
                    p.Name,

                    //
                    Listings = p.ScrapedItems.Select(si => new
                    {
                        StoreName = si.Store!.Name,        // which store
                        si.Url,                            // that store's link
                                                           // latest price for THIS store's listing (newest snapshot first)
                        LatestPrice = si.PriceHistories
                                        .OrderByDescending(ph => ph.CheckedAt)
                                        .Select(ph => ph.Price)
                                        .FirstOrDefault()
                    }).ToList()
                }
            ).ToListAsync();


            return View(query);
        }
    }
}
