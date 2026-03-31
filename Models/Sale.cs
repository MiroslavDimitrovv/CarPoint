using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class Sale : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Кола")]
        public int CarId { get; set; }
        public Car? Car { get; set; }

        [Required]
        [Display(Name = "Клиент")]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [Required]
        [Display(Name = "Дата на продажба")]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Range(1, 1000000)]
        [Display(Name = "Продажна цена")]
        public decimal FinalPrice { get; set; }
        public enum SaleStatus
        {
            Pending = 1,  
            Completed = 2,  
            Cancelled = 3   
        }

        [Display(Name = "Статус")]
        public SaleStatus Status { get; set; } = SaleStatus.Pending;
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SaleDate.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "Датата на продажба не може да е в бъдещето.",
                    new[] { nameof(SaleDate) });
            }

            if (Car != null && Car.Type != Car.ListingType.ForSale)
            {
                yield return new ValidationResult(
                    "Тази кола не е предназначена за продажба.",
                    new[] { nameof(CarId) });
            }
        }
    }
}
