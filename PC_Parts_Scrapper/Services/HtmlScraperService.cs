using HtmlAgilityPack;
namespace PC_Parts_Scrapper.Services
{
    public class HtmlScraperService
    {
        public async Task Czone()
        {
            var doc = new HtmlDocument();
            doc.Load("ProcessorPricesinPakistan.htm");
            //web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            //HtmlDocument doc = await web.LoadFromWebAsync("https://www.czone.com.pk/processors-pakistan-ppt.85.aspx");
            //Console.WriteLine(doc.DocumentNode.OuterHtml.Length);
            //var pageTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();
            //Console.WriteLine($"Page Title: {pageTitle}");
            var productsNds = doc.DocumentNode.SelectNodes("//div[contains(@class,'content-wrapper')]");

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
