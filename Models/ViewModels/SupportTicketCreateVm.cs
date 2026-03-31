using System.ComponentModel.DataAnnotations;
using CarPoint.Models;

namespace CarPoint.Models.ViewModels.Support
{
    public class SupportTicketCreateVm
    {
        [Display(Name = "Име")]
        public string? GuestName { get; set; }

        [Display(Name = "Имейл")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        public string? GuestEmail { get; set; }

        [Display(Name = "Тема")]
        [Required(ErrorMessage = "Въведи тема.")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "Темата трябва да е между {2} и {1} символа.")]
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Категория")]
        [Required]
        public TicketCategory Category { get; set; } = TicketCategory.General;

        [Display(Name = "Приоритет")]
        [Required]
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;

        [Display(Name = "Описание")]
        [Required(ErrorMessage = "Опиши проблема.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Описанието трябва да е между {2} и {1} символа.")]
        public string Description { get; set; } = string.Empty;
    }
}
