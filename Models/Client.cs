using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Име")]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Фамилия")]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Телефон")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Имейл")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "ЕГН")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "ЕГН трябва да е точно 10 цифри.")]
        public string? Egn { get; set; }

        [Display(Name = "Адрес")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Display(Name = "Дата на регистрация")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Display(Name = "Клиент")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
