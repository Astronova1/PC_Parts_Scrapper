using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.ViewModels;
using System.Net.Http.Json;

namespace PC_Parts_Scrapper.Services;

public class PredictionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<PredictionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(config["PredictionService:BaseUrl"]
            ?? "http://localhost:8000");
        _httpClient.Timeout = TimeSpan.FromSeconds(
            config.GetValue<int>("PredictionService:TimeoutSeconds", 60));
    }

    public async Task<PredictionResponseDto?> GetPredictionAsync(
        int scrapedItemId,
        string productName,
        List<(DateTime CheckedAt, decimal Price)> history,
        int forecastDays = 7)
    {
        if (history.Count < 5)
        {
            _logger.LogWarning(
                "Insufficient data ({Count} points) for scraped_item_id={Id}",
                history.Count, scrapedItemId);
            return null;
        }

        var request = new PredictionRequestDto
        {
            ScrapedItemId = scrapedItemId,
            ProductName = productName,
            History = history
                .Select(h => new PricePointDto { Ds = h.CheckedAt, Y = h.Price })
                .ToList(),
            ForecastDays = forecastDays
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/predict", request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Prediction service returned {Status}: {Body}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PredictionResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call prediction service");
            return null;
        }
    }
}