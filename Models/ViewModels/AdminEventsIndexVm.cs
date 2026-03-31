namespace CarPoint.Models.ViewModels
{
    public class AdminEventsIndexVm
    {
        public string? Q { get; set; }
        public string? Type { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public List<string> Types { get; set; } = new();
        public List<CarPoint.Models.AdminEvent> Rows { get; set; } = new();
    }
}
