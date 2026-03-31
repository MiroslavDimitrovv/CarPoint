using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class Rental : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Кола")]
        public int CarId { get; set; }
        public Car? Car { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Display(Name = "Клиент")]
        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Начална дата")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Крайна дата")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Цена / ден")]
        public decimal PricePerDay { get; set; }

        [Display(Name = "Дни")]
        public int Days { get; set; }

        [Required]
        public OfficeLocation PickupOffice { get; set; }

        [Required]
        public OfficeLocation ReturnOffice { get; set; }

        [Display(Name = "Обща цена")]
        public decimal TotalPrice { get; set; }

        public enum RentalStatus
        {
            Active = 1,
            Completed = 2,
            Cancelled = 3,
            ReleasedByOperator = 4
        }

        [Display(Name = "Статус")]
        public RentalStatus Status { get; set; } = RentalStatus.Active;

        public enum PaymentMethod
        {
            CashOnPickup = 1,
            CardPrepay = 2
        }

        public bool IsPaid { get; set; }

        public DateTime? PaidAt { get; set; }


        [Display(Name = "Метод на плащане")]
        public PaymentMethod PayMethod { get; set; } = PaymentMethod.CashOnPickup;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Крайната дата не може да е преди началната.",
                    new[] { nameof(EndDate) });
            }

            if (StartDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Началната дата не може да е в миналото.",
                    new[] { nameof(StartDate) });
            }
        }
    }
}
