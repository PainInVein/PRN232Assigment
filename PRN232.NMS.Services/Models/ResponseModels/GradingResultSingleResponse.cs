namespace PRN232.NMS.Services.Models.ResponseModels
{
    public class GradingResultSingleResponse
    {
        public string ProjectFolder { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Score { get; set; }
        public List<string> Logs { get; set; } = new();
        public DateTime FinishedAt { get; set; }
    }
}
