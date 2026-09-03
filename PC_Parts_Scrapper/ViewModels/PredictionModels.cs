namespace PC_Parts_Scrapper.ViewModels
{
    public class PricePointDto
    {
        public DateTime Ds { get; set; }
        public decimal Y { get; set; }
    }

    public class PredictionRequestDto
    {
        public int ScrapedItemId { get; set; }
        public string? ProductName { get; set; }
        public List<PricePointDto> History { get; set; } = new();
        public int ForecastDays { get; set; } = 7;
    }

    public class ForecastPointDto
    {
        public DateTime Ds { get; set; }
        public decimal Yhat { get; set; }
        public decimal YhatLower { get; set; }
        public decimal YhatUpper { get; set; }
    }

    public class ModelInfoDto
    {
        public int DataPoints { get; set; }
        public string DateRange { get; set; } = "";
        public string TrendDirection { get; set; } = "";
        public string ConfidenceInterval { get; set; } = "";
    }

    public class PredictionResponseDto
    {
        public int ScrapedItemId { get; set; }
        public string? ProductName { get; set; }
        public List<ForecastPointDto> Forecasts { get; set; } = new();
        public ModelInfoDto ModelInfo { get; set; } = new();
    }
}
