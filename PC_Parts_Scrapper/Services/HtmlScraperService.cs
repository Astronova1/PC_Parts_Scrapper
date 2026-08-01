using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Playwright;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
namespace PC_Parts_Scrapper.Services
{
    public class HtmlScraperService
    {
        private readonly PcPartsContext _pc_parts_Context;
        public HtmlScraperService(PcPartsContext pc_parts_Context)
        {
            _pc_parts_Context = pc_parts_Context;
        }

        public async Task<Store> createOrFind_Store(string name, Uri url)
        {
            var store = await _pc_parts_Context.Stores.FirstOrDefaultAsync(s=>s.Name == name);    

            if (store == null)
            {
                Store s = new Store {Name = name, URL = url};
                _pc_parts_Context.Add(s);
                await _pc_parts_Context.SaveChangesAsync();
                return s;
            }
            
            return store;
        }
        public async Task<Product> createorFind_ScrapProduct(string search_name)
        {
            var product = await _pc_parts_Context.Products.FirstOrDefaultAsync(p => p.Name == search_name);    //search for the name of the product

            if (product == null)
            {
                Product p1  = new Product { Name = search_name };
                _pc_parts_Context.Add(p1);
                await _pc_parts_Context.SaveChangesAsync();
                return p1;

            }
            return product;
        }

        public async Task<ScrapedItem> createOrFind_ScrapItem(int s_id, int p_id, Uri url,string product_Name )
        {
            var s_item = await _pc_parts_Context.ScrapedItems.FirstOrDefaultAsync(s => s.StoreId == s_id && s.Title == product_Name);
            if (s_item == null)
            {
                ScrapedItem s1= new ScrapedItem { StoreId = s_id, ProductId = p_id, Url = url, Title = product_Name };
                _pc_parts_Context.Add(s1);
                await _pc_parts_Context.SaveChangesAsync();
                return s1;
            }
            return s_item;
        }


        public async Task<PriceHistory> createOrFind_History(int _id, decimal _price)
        {
            PriceHistory p1 = new PriceHistory {
                ScrapedItemId = _id,
                Price = _price,
                CheckedAt = DateTimeOffset.UtcNow   // Convert your variable to UTC offset 0
            };

             _pc_parts_Context.Add(p1);
            await  _pc_parts_Context.SaveChangesAsync();
            return p1;
        }

        public string NormalizedString(string input)
        {
            return input.ToLower().Trim().Replace("-", " ");
        }



        public async Task Czone(string link_url, string pattern)
        {           
            using var playwright = await Playwright.CreateAsync();        //here we initilize playwrite
            await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false }); //launc using chromium
            var page = await browser.NewPageAsync();        //open new page
            await page.GotoAsync(link_url);    //navigate to the link

            await page.WaitForSelectorAsync("a.product-title",
            new PageWaitForSelectorOptions { Timeout = 30000 });   // 30 seconds, wait for a REAL title

            string html_con = await page.ContentAsync();                   //store the html in html

            var doc = new HtmlDocument();          
            doc.LoadHtml(html_con);     //load the html structure 

            var productsNds = doc.DocumentNode.SelectNodes("//div[contains(@class,'content-wrapper')]");        //select the product full information

            
            if (productsNds == null)
            {
                Console.WriteLine("No product found");
                return;
            }

            Uri link = new Uri("https://www.czone.com.pk");

            var currentStore = await createOrFind_Store("Czone", link);

            //var existingProducts = await _pc_parts_Context.Products.ToListAsync(); // Fetch existing products from the database

            //var exisitingPNames = from p in existingProducts 
            //                      orderby p.Name.Length descending
            //                      select p; // Get the names of existing products
    
            //string pattern = @"(AMD|Intel)\s+(Core\s+Ultra|Core|Ryzen)\s*([iI]\d|\d)?\s*[-–]?\s*\d{3,5}([a-zA-Z0-9]{1,4})?";
                
            foreach (var pro in productsNds)
            {
                var name = pro.SelectSingleNode(".//a[contains(@class,'product-title')]");
                string cpu_Name = HtmlEntity.DeEntitize(name?.InnerText.Trim() ?? "UNknown");       //select the product title

                Match match = Regex.Match(cpu_Name,pattern, RegexOptions.IgnoreCase);

                if (match.Success) {

                    var cpu = pro.SelectSingleNode(".//div[contains(@class, 'product-price')]");        //product price
                    var url_node = pro.SelectSingleNode(".//div[contains(@class, 'content')]//a");
                    string base_uri = "https://www.czone.com.pk";
                    string href = url_node?.GetAttributeValue("href", "www.czone.com.pk") ?? "";
                    Uri rel_url = new Uri(new Uri(base_uri), href);

                    var baseProduct = match.Value.ToUpper();
                    var product_Name = await createorFind_ScrapProduct(baseProduct);
                    var scrapedItem = await createOrFind_ScrapItem(currentStore.StoreId, product_Name.ProductId ,rel_url, cpu_Name);
                    Console.WriteLine($"CPU: {cpu_Name}");


                    if (cpu != null && !string.IsNullOrWhiteSpace(cpu.InnerText))
                    {                    //check if the cpu price is not null or empty
                        var price = cpu.InnerText.Replace("Rs.", "").Replace(",", "").Trim();            //remove un necessary formating
                        decimal.TryParse(price, out decimal cpu_Price);                                 //convert to decimal
                        if (cpu_Price < 0)
                        {
                            Console.WriteLine("Out Of Stock");
                        }
                        Console.WriteLine($"Price: {cpu_Price}");
                        if (rel_url != null)
                        {
                            Console.WriteLine($"URL: {rel_url}");
                        }

                        var price_History = await createOrFind_History(scrapedItem.ScrapedItemId, cpu_Price);        //create price history with 0 price for now
                    }
                }
            }
        }



        public async Task ZahComputers(string url, string pattern)
        {
            using var playwright = await Playwright.CreateAsync();   //create instance of playwright
            await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });      //use launch firefox 
            var page = await browser.NewPageAsync();        //open new page
            await page.GotoAsync(url);    //navigate to the link 
            string html_con = await page.ContentAsync();                   //store the html in html
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html_con);     //load the html structure

            var products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-element-bottom')]");        //select all product nodes 
            if (products != null)
            {
                Console.WriteLine($"Found {products.Count} products on ZahComputers.");
            }
                Uri link = new Uri("https://www.zahcomputers.pk");
                var curr_store = await createOrFind_Store("ZahComputers", link);

            foreach (var pro in products)
            {
                var name = pro.SelectSingleNode(".//h3[contains(@class,'wd-entities-title')]/a");
                string pro_name = HtmlEntity.DeEntitize(name?.InnerText.Trim() ?? "UNknown");       //select the product title)

                Match match = Regex.Match(pro_name, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var price_node = pro.SelectSingleNode(".//span[contains(@class, 'woocommerce-Price-amount')]/bdi");
                    var url_node = pro.SelectSingleNode(".//a");
                    string base_uri = "https://www.zahcomputers.pk";
                    string href = url_node?.GetAttributeValue("href", "www.zahcomputer.pk") ?? "";
                    Uri rel_url = new Uri(new Uri(base_uri), href);

                    var baseProduct = match.Value.ToUpper();
                    var product_Name = await createorFind_ScrapProduct(baseProduct);
                    var scrapedItem = await createOrFind_ScrapItem(curr_store.StoreId, product_Name.ProductId, rel_url, pro_name);
                    Console.WriteLine($"CPU: {pro_name}");


                    if (price_node != null && !string.IsNullOrWhiteSpace(price_node.InnerText))
                    {                    //check if the cpu price is not null or empty
                        var price = Regex.Replace(price_node.InnerText, @"[^\d,\.]","".Replace("Rs.", "").Replace(",", "").Trim());            //remove un necessary formating

                        decimal.TryParse(price, out decimal cpu_Price);                                 //convert to decimal
                        if (cpu_Price < 0)
                        {
                            Console.WriteLine("Out Of Stock");
                        }
                        Console.WriteLine($"Price: {cpu_Price}");
                        if (rel_url != null)
                        {
                            Console.WriteLine($"URL: {rel_url}");
                        }

                        var price_History = await createOrFind_History(scrapedItem.ScrapedItemId, cpu_Price);        //create price history with 0 price for now
                    }

                }

            }
        }
    }
}