using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class AdminEvent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string Type { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(4000)]
        public string? Details { get; set; }

        [StringLength(450)]
        public string? ActorUserId { get; set; }

        [StringLength(256)]
        public string? ActorEmail { get; set; }

        [StringLength(450)]
        public string? TargetUserId { get; set; }

        [StringLength(256)]
        public string? TargetEmail { get; set; }

        public int? CarId { get; set; }
        public int? RentalId { get; set; }

        [StringLength(64)]
        public string? Ip { get; set; }

        [StringLength(256)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
