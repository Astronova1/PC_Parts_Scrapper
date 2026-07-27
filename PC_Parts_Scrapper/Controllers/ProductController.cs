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

        public async Task<IActionResult> getProducts()
        {

            var query = from p in _context.Products
                        from si in p.ScrapedItems
                        where p.Name != null


            return View();
        }
    }
}
