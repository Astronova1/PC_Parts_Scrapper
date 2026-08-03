using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using System.Text.RegularExpressions;

namespace PC_Parts_Scrapper.Services
{
    public class HtmlScraperService
    {
        private readonly PcPartsContext _pc_parts_Context;

        public HtmlScraperService(PcPartsContext pc_parts_Context)
        {
            _pc_parts_Context = pc_parts_Context;
        }

        #region Database Helper Methods

        public async Task<Store> createOrFind_Store(string name, Uri url)
        {
            var store = await _pc_parts_Context.Stores.FirstOrDefaultAsync(s => s.Name == name);
            if (store == null)
            {
                Store s = new Store { Name = name, URL = url };
                _pc_parts_Context.Add(s);
                await _pc_parts_Context.SaveChangesAsync();
                return s;
            }
            return store;
        }

        public async Task<Product> createorFind_ScrapProduct(string search_name)
        {
            var product = await _pc_parts_Context.Products.FirstOrDefaultAsync(p => p.Name == search_name);
            if (product == null)
            {
                Product p1 = new Product { Name = search_name };
                _pc_parts_Context.Add(p1);
                await _pc_parts_Context.SaveChangesAsync();
                return p1;
            }
            return product;
        }

        public async Task<ScrapedItem> createOrFind_ScrapItem(int s_id, int p_id, Uri url, string product_Name)
        {
            var s_item = await _pc_parts_Context.ScrapedItems
                .FirstOrDefaultAsync(s => s.StoreId == s_id && s.Title == product_Name);

            if (s_item == null)
            {
                ScrapedItem s1 = new ScrapedItem
                {
                    StoreId = s_id,
                    ProductId = p_id,
                    Url = url,
                    Title = product_Name
                };
                _pc_parts_Context.Add(s1);
                await _pc_parts_Context.SaveChangesAsync();
                return s1;
            }
            return s_item;
        }

        public async Task<PriceHistory> createOrFind_History(int _id, decimal _price)
        {
            PriceHistory p1 = new PriceHistory
            {
                ScrapedItemId = _id,
                Price = _price,
                CheckedAt = DateTimeOffset.UtcNow
            };

            _pc_parts_Context.Add(p1);
            await _pc_parts_Context.SaveChangesAsync();
            return p1;
        }

        private async Task<T> SafeEvaluateAsync<T>(IPage page, string script, T fallbackValue)
        {
            try
            {
                return await page.EvaluateAsync<T>(script);
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Execution context was destroyed"))
            {
                Console.WriteLine("[Playwright Warning] Context destroyed during evaluate. Re-syncing page state...");
                try
                {
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 5000 });
                }
                catch { /* Ignore timeout */ }
                return fallbackValue;
            }
            catch
            {
                return fallbackValue;
            }
        }

        #endregion

        public async Task Czone(string link_url, string pattern)
        {
            using var playwright = await Playwright.CreateAsync();

            // Store Cloudflare clearance cookies in local directory
            string userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright_profile");

            // we Launch Persistent Context to bypass bot flags
            var context = await playwright.Firefox.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false, // Set to false so you can solve CF once if prompted
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            // Mask automated webdriver property
            await page.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            ");

            try
            {
                Console.WriteLine($"Navigating to CZone: {link_url}");
                await page.GotoAsync(link_url, new PageGotoOptions { Timeout = 60000 });

                // If Cloudflare challenge appears, pause so you can click it manually
                string title = await page.TitleAsync();

                // FIX: Changed 'or' keyword to C# logical OR operator '||'
                if (title.Contains("Just a moment") || title.Contains("Attention Required"))
                {
                    Console.WriteLine("\n=======================================================");
                    Console.WriteLine("Cloudflare challenge detected!");
                    Console.WriteLine("Please solve the Cloudflare box in the browser window.");
                    Console.WriteLine("Waiting up to 60 seconds...");
                    Console.WriteLine("=======================================================\n");

                    await page.WaitForSelectorAsync("a.product-title", new PageWaitForSelectorOptions { Timeout = 60000 });
                    Console.WriteLine("Cloudflare bypassed! Clearance cookie saved to profile.");
                }
                else
                {
                    await page.WaitForSelectorAsync("a.product-title", new PageWaitForSelectorOptions { Timeout = 30000 });
                }

                // Smooth Human-like Incremental Scrolling
                int currentHeight = await SafeEvaluateAsync<int>(page, "document.body.scrollHeight", 0);
                int currentPosition = 0;
                int scrollStep = 500;

                while (currentPosition < currentHeight && currentHeight > 0)
                {
                    currentPosition += scrollStep;

                    await SafeEvaluateAsync<object>(page, $"window.scrollTo(0, {currentPosition});", null);
                    await page.WaitForTimeoutAsync(Random.Shared.Next(300, 600));

                    int newHeight = await SafeEvaluateAsync<int>(page, "document.body.scrollHeight", currentHeight);
                    if (newHeight > 0)
                    {
                        currentHeight = newHeight;
                    }
                }

                // Grab fully expanded HTML DOM
                string html_con = await page.ContentAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html_con);

                var productsNds = doc.DocumentNode.SelectNodes("//div[contains(@class,'content-wrapper')]");

                if (productsNds == null)
                {
                    Console.WriteLine("[Czone] No products found in DOM parsing.");
                    return;
                }

                Console.WriteLine($"[Czone] Found {productsNds.Count} product elements. Processing database inserts...");

                Uri link = new Uri("https://www.czone.com.pk");
                var currentStore = await createOrFind_Store("Czone", link);

                foreach (var pro in productsNds)
                {
                    var name = pro.SelectSingleNode(".//a[contains(@class,'product-title')]");
                    string cpu_Name = HtmlEntity.DeEntitize(name?.InnerText.Trim() ?? "Unknown");

                    Match match = Regex.Match(cpu_Name, pattern, RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        var url_node = pro.SelectSingleNode(".//div[contains(@class, 'content')]//a");
                        string base_uri = "https://www.czone.com.pk";
                        string href = url_node?.GetAttributeValue("href", "") ?? "";
                        Uri rel_url = string.IsNullOrEmpty(href) ? new Uri(base_uri) : new Uri(new Uri(base_uri), href);

                        var baseProduct = match.Value.ToUpper();
                        var product_Name = await createorFind_ScrapProduct(baseProduct);
                        var scrapedItem = await createOrFind_ScrapItem(currentStore.StoreId, product_Name.ProductId, rel_url, cpu_Name);

                        var priceNode = pro.SelectSingleNode(".//div[contains(@class, 'product-price')]");

                        if (priceNode != null && !string.IsNullOrWhiteSpace(priceNode.InnerText))
                        {
                            var priceClean = priceNode.InnerText.Replace("Rs.", "").Replace(",", "").Trim();

                            if (decimal.TryParse(priceClean, out decimal cpu_Price) && cpu_Price > 0)
                            {
                                Console.WriteLine($"[CZone] Saved: {cpu_Name} -> {cpu_Price} PKR");
                                await createOrFind_History(scrapedItem.ScrapedItemId, cpu_Price);
                            }
                            else
                            {
                                Console.WriteLine($"[CZone] Out of Stock / Unparseable Price: {cpu_Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CZone Error] Scraping execution stopped: {ex.Message}");
            }
            finally
            {
                await context.CloseAsync();
            }
        }

        public async Task ZahComputers(string url, string pattern)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(url);
            string html_con = await page.ContentAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html_con);

            var products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-element-bottom')]");

            // FIX: Added null check to prevent NullReferenceException
            if (products == null)
            {
                Console.WriteLine("No products found on ZahComputers.");
                return;
            }

            Console.WriteLine($"Found {products.Count} products on ZahComputers.");

            Uri link = new Uri("https://www.zahcomputers.pk");
            var curr_store = await createOrFind_Store("ZahComputers", link);

            foreach (var pro in products)
            {
                var name = pro.SelectSingleNode(".//h3[contains(@class,'wd-entities-title')]/a");
                string pro_name = HtmlEntity.DeEntitize(name?.InnerText.Trim() ?? "Unknown");

                Match match = Regex.Match(pro_name, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var price_node = pro.SelectSingleNode(".//span[contains(@class, 'woocommerce-Price-amount')]/bdi");
                    var url_node = pro.SelectSingleNode(".//a");
                    string base_uri = "https://www.zahcomputers.pk";
                    string href = url_node?.GetAttributeValue("href", "") ?? "";
                    Uri rel_url = string.IsNullOrEmpty(href) ? new Uri(base_uri) : new Uri(new Uri(base_uri), href);

                    var baseProduct = match.Value.ToUpper();
                    var product_Name = await createorFind_ScrapProduct(baseProduct);
                    var scrapedItem = await createOrFind_ScrapItem(curr_store.StoreId, product_Name.ProductId, rel_url, pro_name);

                    if (price_node != null && !string.IsNullOrWhiteSpace(price_node.InnerText))
                    {
                        // FIX: Clean regex replacement to extract numeric digits cleanly
                        string cleanPriceText = Regex.Replace(price_node.InnerText, @"[^\d.]", "");

                        if (decimal.TryParse(cleanPriceText, out decimal cpu_Price) && cpu_Price > 0)
                        {
                            Console.WriteLine($"[ZahComputers] Item: {pro_name} | Price: {cpu_Price} PKR");
                            await createOrFind_History(scrapedItem.ScrapedItemId, cpu_Price);
                        }
                        else
                        {
                            Console.WriteLine($"[ZahComputers] Out of Stock / Unparseable Price: {pro_name}");
                        }
                    }
                }
            }
        }
    }
}