using PC_Parts_Scrapper.Data;
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
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<PcPartsContext>(); //this is scoped Db Context
                    Console.WriteLine("ScrapperWorker created");
                    var scraper = scope.ServiceProvider.GetRequiredService<HtmlScraperService>();  //use Scrapper in the scope
                    Console.WriteLine("Starting CZone Scrape...");
                    await scraper.Czone();
                    Console.WriteLine("CZone Scrape Completed!");
                }  //the database scope ends here and the scope is disposed now
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);   //run after 6 hours and shutdown if request by stoppingToken
            }
        } 
    }
}
