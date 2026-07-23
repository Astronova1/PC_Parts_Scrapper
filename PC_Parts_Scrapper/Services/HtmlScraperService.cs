using HtmlAgilityPack;
using Microsoft.Playwright;
namespace PC_Parts_Scrapper.Services
{
    public class HtmlScraperService
    {
        public async Task Czone()
        {
           
            //web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            //HtmlDocument doc = await web.LoadFromWebAsync("https://www.czone.com.pk/processors-pakistan-ppt.85.aspx");
            //Console.WriteLine(doc.DocumentNode.OuterHtml.Length);
            //var pageTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();
            //Console.WriteLine($"Page Title: {pageTitle}");
            using var playwright = await Playwright.CreateAsync();        //here we initilize playwrite
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false }); //launc using chromium
            var page = await browser.NewPageAsync();        //open new page
            await page.GotoAsync("https://www.czone.com.pk/processors-pakistan-ppt.85.aspx");     //navigate to the link

            string html_con = await page.ContentAsync();                   //store the html in html
            //var productsNds = doc.DocumentNode.SelectNodes("//div[contains(@class,'content-wrapper')]");

             //var doc = new HtmlDocument();
            //doc.Load("ProcessorPricesinPakistan.htm");

            if (productsNds == null)
            {
                Console.WriteLine("No product found");
                return;
            }
            foreach (var pro in productsNds)
            {
                var name = pro.SelectSingleNode(".//a[contains(@class,'product-title')]");
                string cpu_Name = HtmlEntity.DeEntitize(name?.InnerText.Trim() ?? "UNknown");
                
                Console.WriteLine($"CPU: {cpu_Name}");
   
                var cpu = pro.SelectSingleNode(".//div[contains(@class, 'product-price')]");
                if (cpu != null && !string.IsNullOrWhiteSpace(cpu.InnerText )) {
                    var price = cpu.InnerText.Replace("Rs.", "").Replace(",","").Trim();
                    decimal.TryParse(price, out decimal cpu_Price);
                    Console.WriteLine($"Price: {cpu_Price}");
                }
               
                
            }

        }
    }
}
