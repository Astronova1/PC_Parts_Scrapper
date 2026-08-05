using PC_Parts_Scrapper.Data;
using System.Text.RegularExpressions;
namespace PC_Parts_Scrapper.Services
{
    public class ScrapperWorker : BackgroundService   //to make a background service that runs in the background without distrubing the application
    {
        private readonly IServiceScopeFactory _scopeFactory;    //we create scope for database connection b/c db is scopped and ScrapperWorker is singliton so it would give error
        public ScrapperWorker (IServiceScopeFactory scopeFactory) //scope makes sures when scope is disposed the db connection also dispossed and fixes singliton error
        {
            _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<PcPartsContext>(); //this is scoped Db Context 
                        Console.WriteLine("ScrapperWorker created");
                        var scraper = scope.ServiceProvider.GetRequiredService<HtmlScraperService>();  //use Scrapper in the scope
                        Console.WriteLine("Starting Scraping service");
                        await scraper.ScrapStores();
                    }  //the database scope ends here and the scope is disposed now
                    Console.WriteLine("Scraping service completed");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error in ScrapperWorker: {ex.Message}");
                }
                Console.WriteLine("ScrapperWorker waiting 6 hours before starting again");    
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);  //wait for 6 hours before starting again
            }
        }
    }
}
