using CarPoint.Models;

namespace CarPoint.Models.ViewModels.Support
{
    public class AdminSupportDetailsVm
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string Subject { get; set; } = "";
        public string Description { get; set; } = "";

        public TicketCategory Category { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }

        public string? AdminNote { get; set; }
        public TicketStatus NewStatus { get; set; }
        public TicketPriority NewPriority { get; set; }
        public string? NewAdminNote { get; set; }
    }
}
