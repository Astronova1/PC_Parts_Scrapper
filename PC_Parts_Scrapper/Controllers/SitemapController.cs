using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using System.Text;

namespace PC_Parts_Scrapper.Controllers
{
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private readonly PcPartsContext _context;
        private const string BaseUrl = "https://findpcparts.app";

        public SitemapController(PcPartsContext context) => _context = context;

        [HttpGet("/sitemap.xml")]
        public async Task<IActionResult> GetSitemap()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // Static pages
            sb.AppendLine(Entry("/", DateTime.UtcNow, "daily", "1.0"));
            sb.AppendLine(Entry("/about", DateTime.UtcNow, "monthly", "0.3"));

            var categoryIds = await _context.Categories.Select(c => c.CategoryId).ToListAsync();
            foreach (var id in categoryIds)
                sb.AppendLine(Entry($"/?category={id}", DateTime.UtcNow, "daily", "0.8"));

            var products = await _context.Products
                .Select(p => new
                {
                    p.ProductId,
                    LastMod = p.ScrapedItems
                        .SelectMany(si => si.PriceHistories)
                        .Max(ph => (DateTimeOffset?)ph.CheckedAt)
                })
                .ToListAsync();

            foreach (var p in products)
                sb.AppendLine(Entry($"/products/{p.ProductId}",
                            (p.LastMod ?? DateTimeOffset.UtcNow).UtcDateTime, "daily", "0.9"));

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml");
        }

        private static string Entry(string path, DateTime lastMod, string freq, string priority) =>
            $"""
             <url>
               <loc>{BaseUrl}{path}</loc>
               <lastmod>{lastMod:yyyy-MM-dd}</lastmod>
               <changefreq>{freq}</changefreq>
               <priority>{priority}</priority>
             </url>
             """;
    }
}
