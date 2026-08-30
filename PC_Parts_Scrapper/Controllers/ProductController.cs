using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.ViewModels;
namespace PC_Parts_Scrapper.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly PcPartsContext _context;  //implement injecting context in the controller constructor to access the database
        public ProductController(PcPartsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> getProducts([FromQuery] int? category) //get category all or from url params if avaiable from the
                                                                               //frontend ProductList.jsx
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
                   ScrapedItemId = si.ScrapedItemId,
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

            [HttpGet("{ScrapedItemId}/history")]
            public async Task<IActionResult> GetPriceHistory(int ScrapedItemId)
            {
                var p_Histroy = await _context.PriceHistory
                    .Where(ph => ph.ScrapedItemId == ScrapedItemId)
                    .OrderBy(ph => ph.CheckedAt)
                    .Select(ph => new
                    {
                        Price = ph.Price,
                        CheckedAt = ph.CheckedAt,
                        StoreName = ph.ScrapedItem!.Store!.Name,
                    }).ToListAsync();
                if (p_Histroy == null || !(p_Histroy.Any()) || p_Histroy.Count == 0)
                {
                    return NotFound();
                }
                return Ok(p_Histroy);
            }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == id)
                .Select(p=> new
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    LatestPrice = p.ScrapedItems
                                   .Where(si => si.ScrapedItemId == id)
                                   .SelectMany(si => si.PriceHistories)
                                   .OrderByDescending(ph => ph.CheckedAt)
                                   .Select(ph => ph.Price)
                                   .FirstOrDefault()
                })
                .FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
    }
}
