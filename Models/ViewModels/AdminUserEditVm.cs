using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models.ViewModels
{
    public class AdminUserEditVm
    {
        public string UserId { get; set; } = "";
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Името е задължително.")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Фамилията е задължителна.")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Телефонът е задължителен.")]
        public string PhoneNumber { get; set; } = "";
    }
}
