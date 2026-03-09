namespace PRN232.NMS.Services
{
    public class GradingRequest
    {
        public int StudentId { get; set; }
        public string ProjectFolder { get; set; } = null!;  // C:\students\student123
    }
}
