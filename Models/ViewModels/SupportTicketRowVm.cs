using CarPoint.Models;

namespace CarPoint.Models.ViewModels.Support
{
    public class SupportTicketRowVm
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Subject { get; set; } = "";
        public TicketCategory Category { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public bool HasUnreadReply { get; set; }
    }
}
