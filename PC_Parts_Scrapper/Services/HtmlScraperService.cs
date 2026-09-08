using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Helpers;
using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.Utils;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace PC_Parts_Scrapper.Services
{
    public class HtmlScraperService
    {
        private readonly PcPartsContext _pc_parts_Context;
        private readonly IConfiguration _configuration;


        public HtmlScraperService(PcPartsContext pc_parts_Context, IConfiguration configuration)
        {
            _pc_parts_Context = pc_parts_Context;
            _configuration = configuration;
        }

        public async Task CheckPriceAlertsAsync()
        {
            var productsWithAlerts = await _pc_parts_Context.Products
                .Where(p => p.ScrapedItems.Any(si => si.PriceHistories.Any()))
                .Include(p => p.ScrapedItems)
                    .ThenInclude(si => si.PriceHistories)
                .Where(p => p.PriceAlerts.Any(a => a.IsActive))
                .ToListAsync();

            foreach (var product in productsWithAlerts)
            {
                var latestPrice = product.ScrapedItems
                    .SelectMany(si => si.PriceHistories)
                    .OrderByDescending(ph => ph.CheckedAt)
                    .FirstOrDefault();

                if (latestPrice == null) continue;

                foreach (var alert in product.PriceAlerts)
                {
                    if (latestPrice.Price <= alert.TargetPrice)
                    {
                        alert.IsActive = false;
                        alert.ActiveAt = DateTime.UtcNow;

                        var notification = new Notification
                        {
                            UserId = alert.UserId,
                            PriceAlertId = alert.Id,
                            Title = $"Price Drop Alert: {product.Name}",
                            Message = $"{product.Name} is now {latestPrice.Price:C} (below your target of {alert.TargetPrice:C})",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };

                        _pc_parts_Context.Notifications.Add(notification);
                    }
                }
            }

            await _pc_parts_Context.SaveChangesAsync();
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

        public async Task<Product> createorFind_ScrapProduct(string search_name, string category_name)
        {
            var product = await _pc_parts_Context.Products.FirstOrDefaultAsync(p => p.Name == search_name);
            if (product == null)
            {
                var category = await _pc_parts_Context.Categories.FirstOrDefaultAsync(c => c.CategoryName == category_name);
                if (category == null)
                {
                    category = new Category { CategoryName = category_name };
                    _pc_parts_Context.Add(category);
                    await _pc_parts_Context.SaveChangesAsync();
                }
                Console.WriteLine($"[Database] Product '{search_name}' not found. Creating new entry.");
                Product p1 = new Product
                {
                    Name = search_name,
                    CategoryId = category.CategoryId,
                    GraphicsType = ProductParser.ExtractGraphicsType(search_name, category_name)
                };
                _pc_parts_Context.Add(p1);
                await _pc_parts_Context.SaveChangesAsync();
                return p1;
            }
            else if (product.GraphicsType == null)
            {
                product.GraphicsType = ProductParser.ExtractGraphicsType(search_name, category_name);
                if (product.GraphicsType != null)
                    await _pc_parts_Context.SaveChangesAsync();
            }
            return product;
        }

        public async Task<ScrapedItem> createOrFind_ScrapItem(int s_id, int p_id, Uri url, string product_Name)
        {
            var s_item = await _pc_parts_Context.ScrapedItems
                .FirstOrDefaultAsync(s => s.StoreId == s_id && s.Title == product_Name);

            var brand = ProductParser.ExtractBrand(product_Name);

            if (s_item == null)
            {
                Console.WriteLine($"[Database] Item '{product_Name}' not found. Creating new entrys");
                ScrapedItem s1 = new ScrapedItem
                {
                    StoreId = s_id,
                    ProductId = p_id,
                    Url = url,
                    Title = product_Name,
                    Brand = brand
                };
                _pc_parts_Context.Add(s1);
                await _pc_parts_Context.SaveChangesAsync();
                return s1;
            }

            if (s_item.Brand == null && brand != null)
            {
                s_item.Brand = brand;
                await _pc_parts_Context.SaveChangesAsync();
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

        private static bool IsZahListingOutOfStock(HtmlNode productNode)
        {
            if (ContainsOutOfStockLabel(productNode))
                return true;

            var card = productNode;
            for (int i = 0; i < 4 && card.ParentNode != null; i++)
            {
                card = card.ParentNode;
                var cls = card.GetAttributeValue("class", "");
                if (cls.Contains("product-wrapper") || cls.Contains("product-grid-item"))
                {
                    var titles = card.SelectNodes(".//h3[contains(@class, 'wd-entities-title')]");
                    if (titles == null || titles.Count <= 1)
                        return ContainsOutOfStockLabel(card);
                    break;
                }
            }

            return false;
        }

        private static bool ContainsOutOfStockLabel(HtmlNode node)
        {
            var oosLabel = node.SelectSingleNode(
                ".//span[contains(concat(' ', normalize-space(@class), ' '), ' out-of-stock ')]");
            if (oosLabel != null)
                return true;

            var labels = node.SelectNodes(
                ".//*[contains(concat(' ', normalize-space(@class), ' '), ' product-label ')]");
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    var text = Regex.Replace(HtmlEntity.DeEntitize(label.InnerText ?? ""), @"\s+", " ").Trim();
                    if (text.Contains("sold out", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("out of stock", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private async Task SetStockStatusAsync(ScrapedItem item, bool isOutOfStock)
        {
            if (item.IsOutOfStock == isOutOfStock)
                return;

            item.IsOutOfStock = isOutOfStock;
            await _pc_parts_Context.SaveChangesAsync();
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
        public async Task AssignCategoriesToExistingProducts()
        {
            var products = await _pc_parts_Context.Products
                .Where(p => p.Category.CategoryName == null)
                .ToListAsync();

            Console.WriteLine($"Found {products.Count} products without categories.");

            foreach (var product in products)
            {
                string categoryName = AddCategory.Addcategory(product.Name);

                var category = await _pc_parts_Context.Categories
               .FirstOrDefaultAsync(c => c.CategoryName == categoryName);

                if (category == null)
                {
                    category = new Category { CategoryName = categoryName };
                    _pc_parts_Context.Categories.Add(category);
                    await _pc_parts_Context.SaveChangesAsync();
                }

                product.CategoryId = category.CategoryId;
            }

            await _pc_parts_Context.SaveChangesAsync();
            Console.WriteLine("Categories assigned to all products!");
        }

        public async Task BackfillBrandsAndGraphicsTypes()
        {
            var products = await _pc_parts_Context.Products
                .Include(p => p.Category)
                .Where(p => p.GraphicsType == null)
                .ToListAsync();

            int productsUpdated = 0;
            foreach (var product in products)
            {
                var graphicsType = ProductParser.ExtractGraphicsType(product.Name, product.Category?.CategoryName);
                if (graphicsType != null)
                {
                    product.GraphicsType = graphicsType;
                    productsUpdated++;
                }
            }

            var scrapedItems = await _pc_parts_Context.ScrapedItems
                .Where(s => s.Brand == null)
                .ToListAsync();

            int itemsUpdated = 0;
            foreach (var item in scrapedItems)
            {
                var brand = ProductParser.ExtractBrand(item.Title);
                if (brand != null)
                {
                    item.Brand = brand;
                    itemsUpdated++;
                }
            }

            await _pc_parts_Context.SaveChangesAsync();
            Console.WriteLine($"Backfill complete: {productsUpdated}/{products.Count} products got a GraphicsType, {itemsUpdated}/{scrapedItems.Count} scraped items got a Brand.");
        }

        #endregion

        public async Task ScrapStores()
        {
            var cpu_link = "https://www.czone.com.pk/processors-pakistan-ppt.85.aspx";  //CZone website link
            string pattern = @"(?i)(AMD|Intel)\s+(Core\s+Ultra|Core|Ryzen)\s*([iI]\d|\d)?\s*(Threadripper)?\s*(PRO)?\s*[-–]?\s*\d{3,5}([a-zA-Z0-9]{1,4})?";

            var gpu_link = "https://www.czone.com.pk/graphic-cards-pakistan-ppt.154.aspx";  //CZone gpu link
            string pattern_gpu = @"(?i)(RTX|GTX|RX|GT)\s*\d{1,4}\s*(Ti|XT|XTX)?";

            await Czone(cpu_link, pattern, "CPU");
            await Czone(gpu_link, pattern_gpu, "GPU");
            Console.WriteLine("CZone Scrape Completed!");

            Console.WriteLine("Starting ZahComputers Scrape...");
            var zah_link_cpu = "https://zahcomputers.pk/category/processors/";  //ZahComputers website link
            var zah_link_gpu = "https://zahcomputers.pk/category/graphics-cards/";  //ZahComputers gpu link
            await ZahComputers(zah_link_cpu, pattern, "CPU");
            await ZahComputers(zah_link_gpu, pattern_gpu, "GPU");
            Console.WriteLine("ZahComputers Scrape Complete");
        }

        public async Task Czone(string link_url, string pattern, string category_name)
        {
            using var playwright = await Playwright.CreateAsync();

            string userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "playwright_profile");
            bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            var context = await playwright.Firefox.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = true,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                FirefoxUserPrefs = new Dictionary<string, object>
        {
            { "security.sandbox.content.level", 0 }
        }
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            await page.AddInitScriptAsync(@"
        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
    ");

            try
            {
                Console.WriteLine($"Navigating to CZone: {link_url}");
                await page.GotoAsync(link_url, new PageGotoOptions { Timeout = 60000 });

                string title = await page.TitleAsync();

                if (title.Contains("Just a moment") || title.Contains("Attention Required"))
                {
                    Console.WriteLine("Cloudflare challenge detected!");
                    Console.WriteLine("Please solve the Cloudflare box in the browser window.");
                    Console.WriteLine("Waiting up to 60 seconds...");
                    await page.WaitForSelectorAsync("a.product-title", new PageWaitForSelectorOptions { Timeout = 60000 });
                    Console.WriteLine("Cloudflare bypassed! Clearance cookie saved to profile.");
                }
                else
                {
                    await page.WaitForSelectorAsync("a.product-title", new PageWaitForSelectorOptions { Timeout = 30000 });
                }

                int maxScrollAttempts = 30;
                int stableCount = 0;
                int previousCount = 0;
                DateTime startTime = DateTime.UtcNow;
                int maxTotalSeconds = 120;

                while (maxScrollAttempts > 0 && (DateTime.UtcNow - startTime).TotalSeconds < maxTotalSeconds)
                {
                    int currentCount = await SafeEvaluateAsync<int>(page, @"document.querySelectorAll('div.content-wrapper').length", 0);
                    Console.WriteLine($"[CZone] Product count so far: {currentCount}");

                    if (currentCount == previousCount)
                    {
                        stableCount++;
                        if (stableCount >= 2)
                        {
                            Console.WriteLine("[CZone] No new products after two consecutive scrolls. Stopping.");
                            break;
                        }
                    }
                    else
                    {
                        stableCount = 0;
                    }
                    previousCount = currentCount;

                    int scrollStep = Random.Shared.Next(400, 700);
                    await SafeEvaluateAsync<object>(page, $"window.scrollBy(0, {scrollStep});", null);

                    try
                    {
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 5000 });
                    }
                    catch
                    {
                    }

                    // Human-like random mouse move and pause
                    await page.Mouse.MoveAsync(Random.Shared.Next(200, 500), Random.Shared.Next(200, 500));
                    await page.WaitForTimeoutAsync(Random.Shared.Next(1000, 1800));

                    maxScrollAttempts--;
                }

                int finalCount = await SafeEvaluateAsync<int>(page, @"document.querySelectorAll('div.content-wrapper').length", 0);
                Console.WriteLine($"[CZone] Final product count after scrolling: {finalCount}");

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
                        var product_Name = await createorFind_ScrapProduct(baseProduct, category_name);
                        var scrapedItem = await createOrFind_ScrapItem(currentStore.StoreId, product_Name.ProductId, rel_url, cpu_Name);

                        var priceNode = pro.SelectSingleNode(".//div[contains(@class, 'product-price')]");

                        bool hasValidPrice = false;
                        if (priceNode != null && !string.IsNullOrWhiteSpace(priceNode.InnerText))
                        {
                            var priceClean = priceNode.InnerText.Replace("Rs.", "").Replace(",", "").Trim();

                            if (decimal.TryParse(priceClean, out decimal cpu_Price) && cpu_Price > 0)
                            {
                                hasValidPrice = true;
                                Console.WriteLine($"[CZone] Saved: {cpu_Name} -> {cpu_Price} PKR");
                                await createOrFind_History(scrapedItem.ScrapedItemId, cpu_Price);
                            }
                            else
                            {
                                Console.WriteLine($"[CZone] Out of Stock / Unparseable Price: {cpu_Name}");
                            }
                        }

                        // "Sold out" / "Out of stock" markers can appear even when a price is shown.
                        var proText = Regex.Replace(pro.InnerText ?? "", @"\s+", " ");
                        bool markedOutOfStock =
                            proText.Contains("out of stock", StringComparison.OrdinalIgnoreCase) ||
                            proText.Contains("sold out", StringComparison.OrdinalIgnoreCase);

                        await SetStockStatusAsync(scrapedItem, markedOutOfStock || !hasValidPrice);
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

        public async Task ZahComputers(string url, string pattern, string category_name)
        {
            string flareSolverrUrl = _configuration["FlareSolverr:BaseUrl"] ?? "http://localhost:8191/v1";
            using var client = new HttpClient();
            string sessionId = $"zah_{Guid.NewGuid():N}";

            // --- Create a persistent session ---
            var createSessionPayload = new { cmd = "sessions.create", session = sessionId };
            await client.PostAsync(flareSolverrUrl,
                new StringContent(System.Text.Json.JsonSerializer.Serialize(createSessionPayload),
                System.Text.Encoding.UTF8, "application/json"));

            Uri link = new Uri("https://www.zahcomputers.pk");
            var curr_store = await createOrFind_Store("ZahComputers", link);

            int pageNum = 1;
            int totalProcessed = 0;
            int maxPages = 50;

            try
            {
                while (pageNum <= maxPages)
                {
                    string pagedUrl = pageNum == 1 ? url : $"{url.TrimEnd('/')}/page/{pageNum}/";
                    Console.WriteLine($"[ZahComputers] Fetching page {pageNum}: {pagedUrl}");

                    var payload = new
                    {
                        cmd = "request.get",
                        url = pagedUrl,
                        session = sessionId,   // reuse solved cookies
                        maxTimeout = 60000
                    };

                    string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    HtmlNodeCollection products = null;
                    string rawHtml = null;

                    try
                    {
                        var response = await client.PostAsync(flareSolverrUrl, content);
                        var result = await response.Content.ReadFromJsonAsync<FlareSolverrResponse>();

                        if (result?.Status == "ok" && result.Solution != null)
                        {
                            rawHtml = result.Solution.Response;
                            var doc = new HtmlDocument();
                            doc.LoadHtml(rawHtml);
                            products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-list-content') or contains(@class, 'product-element-bottom')]");
                        }
                        else
                        {
                            Console.WriteLine($"[ZahComputers Error] FlareSolverr failed on page {pageNum}: {result?.Message ?? "No message"}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ZahComputers Error] HTTP Request failed on page {pageNum}: {ex.Message}");
                        break;
                    }

                    if (products == null || products.Count == 0)
                    {
                        bool looksLikeChallenge = rawHtml != null &&
                            (rawHtml.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) ||
                             rawHtml.Contains("cf-challenge", StringComparison.OrdinalIgnoreCase) ||
                             rawHtml.Contains("Verify you are human", StringComparison.OrdinalIgnoreCase));

                        if (looksLikeChallenge)
                        {
                            Console.WriteLine($"[ZahComputers] Page {pageNum} returned a Cloudflare challenge, not content. Retrying once...");
                            await Task.Delay(5000);
                            continue;
                        }

                        Console.WriteLine($"[ZahComputers] No products on page {pageNum} (confirmed real end). Stopping.");
                        break;
                    }

                    Console.WriteLine($"[ZahComputers] Page {pageNum}: {products.Count} products found. Processing...");

                    foreach (var pro in products)
                    {
                        var name = pro.SelectSingleNode(".//h3[contains(@class,'wd-entities-title')]/a");
                        string pro_name = HtmlEntity.DeEntitize(name?.InnerText.Trim() ?? "Unknown");

                        Match match = Regex.Match(pro_name, pattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var salePriceNode = pro.SelectSingleNode(".//ins//span[contains(@class, 'woocommerce-Price-amount')]/bdi");
                            var originalPriceNode = pro.SelectSingleNode(".//del//span[contains(@class, 'woocommerce-Price-amount')]/bdi");
                            var price_node = salePriceNode ?? pro.SelectSingleNode(".//span[contains(@class, 'woocommerce-Price-amount')]/bdi");
                            var url_node = pro.SelectSingleNode(".//a");
                            string base_uri = "https://www.zahcomputers.pk";
                            string href = url_node?.GetAttributeValue("href", "") ?? "";
                            Uri rel_url = string.IsNullOrEmpty(href) ? new Uri(base_uri) : new Uri(new Uri(base_uri), href);

                            var baseProduct = match.Value.ToUpper();
                            var product_Name = await createorFind_ScrapProduct(baseProduct, category_name);
                            var scrapedItem = await createOrFind_ScrapItem(curr_store.StoreId, product_Name.ProductId, rel_url, pro_name);

                            bool isOutOfStock = IsZahListingOutOfStock(pro);
                            await SetStockStatusAsync(scrapedItem, isOutOfStock);
                            if (isOutOfStock)
                                Console.WriteLine($"[ZahComputers] Out of stock (price still listed): {pro_name}");

                            if (price_node != null && !string.IsNullOrWhiteSpace(price_node.InnerText))
                            {
                                string cleanPriceText = Regex.Replace(price_node.InnerText, @"[^\d.]", "");

                                if (decimal.TryParse(cleanPriceText, out decimal cpu_Price) && cpu_Price > 0)
                                {
                                    bool isOnSale = salePriceNode != null;
                                    decimal? originalPrice = null;
                                    if (originalPriceNode != null)
                                    {
                                        string cleanOriginal = Regex.Replace(originalPriceNode.InnerText, @"[^\d.]", "");
                                        if (decimal.TryParse(cleanOriginal, out decimal orig) && orig > 0)
                                            originalPrice = orig;
                                    }
                                    if (isOnSale)
                                        Console.WriteLine($"[ZahComputers] SALE: {pro_name} | Was: Rs. {originalPrice} | Now: Rs. {cpu_Price}");
                                    else
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

                    totalProcessed += products.Count;
                    pageNum++;

                    await Task.Delay(2000);
                }
            }
            finally
            {
                // --- Clean up session ---
                var destroySessionPayload = new { cmd = "sessions.destroy", session = sessionId };
                await client.PostAsync(flareSolverrUrl,
                    new StringContent(System.Text.Json.JsonSerializer.Serialize(destroySessionPayload),
                    System.Text.Encoding.UTF8, "application/json"));
            }

            Console.WriteLine($"[ZahComputers] Completed. Total products processed across {pageNum - 1} pages: {totalProcessed}");
        }


    }
}