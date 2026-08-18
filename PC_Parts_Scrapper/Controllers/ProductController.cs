using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.ViewModels;
namespace PC_Parts_Scrapper.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly PcPartsContext _context;  //implement injecting context in the controller constructor to access the database
        public ProductController(PcPartsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> getProducts([FromQuery] int? category)
        {
            var query = _context.Products.AsQueryable();
            if (category.HasValue)
            {
                query = query.Where(p => p.CategoryId == category);
            }
                var result = await query
           .Select(p => new ProductDisplayViewModel
           {
               ProductId = p.ProductId,
               Name = p.Name,
               Listings = p.ScrapedItems.Select(si => new StoreListingViewModel
               {
                   StoreName = si.Store!.Name,
                   Url = si.Url,
                   ItemTitle = si.Title,
                   LatestPrice = si.PriceHistories
                       .OrderByDescending(ph => ph.CheckedAt)
                       .Select(ph => ph.Price)
                       .FirstOrDefault(),
                   CheckedAt = si.PriceHistories
                       .OrderByDescending(ph => ph.CheckedAt)
                       .Select(ph => ph.CheckedAt)
                       .FirstOrDefault()
               }).ToList()
           })
           .ToListAsync();

                return Ok(result);

        }
    }
}
