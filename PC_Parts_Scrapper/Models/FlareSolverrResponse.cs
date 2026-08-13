namespace PC_Parts_Scrapper.Models
{
    public class FlareSolverrResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public FlareSolverrSolution? Solution { get; set; }
    }
    public class FlareSolverrSolution
    {
        public string Response { get; set; } = string.Empty;
        public int Status { get; set; }
    }
}
