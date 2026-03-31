using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class SupportTicketMessage
    {
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        public SupportTicket? Ticket { get; set; }

        [Required]
        public string AuthorUserId { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
