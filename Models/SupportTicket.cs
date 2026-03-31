using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class SupportTicket
    {
        public List<SupportTicketMessage> Messages { get; set; } = new();
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 3)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(4000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TicketCategory Category { get; set; } = TicketCategory.General;

        [Required]
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        [StringLength(4000)]
        public string? AdminNote { get; set; }
    }

    public enum TicketCategory
    {
        [Display(Name = "Общи въпроси")]
        General = 0,
        [Display(Name = "Акаунт")]
        Account = 1,
        [Display(Name = "Наеми")]
        Rentals = 2,
        [Display(Name = "Продажби")]
        Sales = 3,
        [Display(Name = "Плащания")]
        Payments = 4,
        [Display(Name = "Технически проблем")]
        Bug = 5
    }

    public enum TicketPriority
    {
        [Display(Name = "Нисък")]
        Low = 0,
        [Display(Name = "Нормален")]
        Normal = 1,
        [Display(Name = "Висок")]
        High = 2
    }

    public enum TicketStatus
    {
        [Display(Name = "Отворена")]
        Open = 0,
        [Display(Name = "В процес")]
        InProgress = 1,
        [Display(Name = "Затворена")]
        Closed = 2
    }
}
