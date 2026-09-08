using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.Services;
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
        public async Task<IActionResult> getProducts(
            [FromQuery] int? category,
            [FromQuery] string? search,
            [FromQuery] string[]? brands,
            [FromQuery] string[]? graphicsTypes,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? inStockOnly,
            [FromQuery] string? sortBy,
            [FromQuery] string? storeIds,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Products.AsQueryable();
            if (category.HasValue)
            {
                query = query.Where(p => p.CategoryId == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{normalizedSearch}%"));
            }

            if (graphicsTypes != null && graphicsTypes.Length > 0)
            {
                query = query.Where(p => p.GraphicsType != null && graphicsTypes.Contains(p.GraphicsType));
            }

            if (brands != null && brands.Length > 0)
            {
                query = query.Where(p => p.ScrapedItems.Any(si => si.Brand != null && brands.Contains(si.Brand)));
            }

            if (inStockOnly == true)
            {
                query = query.Where(p => p.ScrapedItems.Any(si => !si.IsOutOfStock));
            }

            if (minPrice.HasValue || maxPrice.HasValue)
            {
                query = query.Where(p => p.ScrapedItems.Any(si =>
                    si.PriceHistories.OrderByDescending(ph => ph.CheckedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault() >= (minPrice ?? 0)
                    && si.PriceHistories.OrderByDescending(ph => ph.CheckedAt).Select(ph => (decimal?)ph.Price).FirstOrDefault() <= (maxPrice ?? decimal.MaxValue)));
            }

            if (!string.IsNullOrWhiteSpace(storeIds))
            {
                var storeNames = storeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToLower())
                    .ToArray();

                if (storeNames.Length > 0)
                {
                    query = query.Where(p => p.ScrapedItems.Any(si =>
                        si.Store != null && storeNames.Contains(si.Store.Name.ToLower())));
                }
            }

            // Latest price for a product = lowest current price across its store listings.
            // A listing with no price history contributes null and is ignored by Min().
            query = sortBy switch
            {
                "price_asc" => query
                    .OrderBy(p => p.ScrapedItems
                        .Select(si => si.PriceHistories
                            .OrderByDescending(ph => ph.CheckedAt)
                            .Select(ph => (decimal?)ph.Price)
                            .FirstOrDefault())
                        .Min() ?? decimal.MaxValue)
                    .ThenBy(p => p.ProductId),
                "price_desc" => query
                    .OrderByDescending(p => p.ScrapedItems
                        .Select(si => si.PriceHistories
                            .OrderByDescending(ph => ph.CheckedAt)
                            .Select(ph => (decimal?)ph.Price)
                            .FirstOrDefault())
                        .Min() ?? decimal.MinValue)
                    .ThenBy(p => p.ProductId),
                "latest" => query
                    .OrderByDescending(p => p.ScrapedItems
                        .SelectMany(si => si.PriceHistories)
                        .Max(ph => (DateTimeOffset?)ph.CheckedAt) ?? DateTimeOffset.MinValue)
                    .ThenBy(p => p.ProductId),
                "popularity" => query
                    .OrderByDescending(p => p.PriceAlerts.Count)
                    .ThenBy(p => p.ProductId),
                _ => query.OrderBy(p => p.ProductId)
            };

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var result = await query
           .Skip((page - 1) * pageSize)
           .Take(pageSize)
           .Select(p => new ProductDisplayViewModel
           {
               ProductId = p.ProductId,
               Name = p.Name,
               GraphicsType = p.GraphicsType,
               Listings = p.ScrapedItems.Select(si => new StoreListingViewModel
               {
                   ScrapedItemId = si.ScrapedItemId,
                   StoreName = si.Store!.Name,
                   Url = si.Url,
                   ItemTitle = si.Title,
                   IsOutOfStock = si.IsOutOfStock,
                   Brand = si.Brand,
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

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = result
            });
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterOptions([FromQuery] int? category)
        {
            var itemsQuery = _context.ScrapedItems.AsQueryable();
            if (category.HasValue)
            {
                itemsQuery = itemsQuery.Where(si => si.Product!.CategoryId == category);
            }

            var brands = await itemsQuery
                .Where(si => si.Brand != null)
                .Select(si => si.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            var graphicsTypesQuery = _context.Products.Where(p => p.GraphicsType != null);
            if (category.HasValue)
            {
                graphicsTypesQuery = graphicsTypesQuery.Where(p => p.CategoryId == category);
            }
            var graphicsTypes = await graphicsTypesQuery
                .Select(p => p.GraphicsType!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            var stores = await itemsQuery
                .Where(si => si.Store != null)
                .Select(si => si.Store!.Name)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var latestPrices = await itemsQuery
                .Select(si => si.PriceHistories
                    .OrderByDescending(ph => ph.CheckedAt)
                    .Select(ph => ph.Price)
                    .FirstOrDefault())
                .Where(p => p > 0)
                .ToListAsync();

            return Ok(new
            {
                Brands = brands,
                GraphicsTypes = graphicsTypes,
                Stores = stores,
                MinPrice = latestPrices.Count > 0 ? latestPrices.Min() : 0,
                MaxPrice = latestPrices.Count > 0 ? latestPrices.Max() : 0
            });
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
                                   .FirstOrDefault(),
                    IsOutOfStock = p.ScrapedItems
                                   .Where(si => si.ScrapedItemId == id)
                                   .Select(si => si.IsOutOfStock)
                                   .FirstOrDefault()
                })
                .FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpGet("{scrapedItemId}/predict")]
        public async Task<ActionResult<PredictionResponseDto>> GetPrediction(
    int scrapedItemId,
    [FromServices] PredictionService predictionService,
    [FromQuery] int days = 7)
        {
            var scrapedItem = await _context.ScrapedItems
                .Include(si => si.Product)
                .FirstOrDefaultAsync(si => si.ScrapedItemId == scrapedItemId);

            if (scrapedItem == null) return NotFound();

            var history = await _context.PriceHistory
                .Where(ph => ph.ScrapedItemId == scrapedItemId)
                .OrderBy(ph => ph.CheckedAt)
                .ToListAsync();

            if (history.Count < 5)
                return BadRequest(new
                {
                    error = "Insufficient historical data",
                    dataPoints = history.Count,
                    minimumRequired = 5
                });

            var historyData = history
                .Select(h => (h.CheckedAt.UtcDateTime, h.Price))
                .ToList();

            var prediction = await predictionService.GetPredictionAsync(
                scrapedItemId,
                scrapedItem.Product?.Name ?? "",
                historyData,
                days
            );

            if (prediction == null)
                return StatusCode(500, "Prediction service unavailable");

            return Ok(prediction);
        }
    }
}
