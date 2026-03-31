using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class Car : IValidatableObject
    {
        public enum ListingType
        {
            ForSale = 1,
            ForRent = 2
        }

        public enum StatusType
        {
            Available = 1,
            Rented = 2,
            Sold = 3,
            InService = 4,
            Unavailable = 5
        }

        public int Id { get; set; }

        [Required]
        [Display(Name = "Марка")]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Модел")]
        public string Model { get; set; } = string.Empty;

        [Display(Name = "Снимка (файл)")]
        public string? ImageFileName { get; set; }

        [Range(1920, 2100)]
        [Display(Name = "Година")]
        public int Year { get; set; }

        [Display(Name = "Описание")]
        [StringLength(1000, MinimumLength = 10)]
        public string? Description { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Километраж")]
        public int Mileage { get; set; }

        [Required]
        [Display(Name = "Двигател")]
        public string Engine { get; set; } = string.Empty;

        [Range(50, 2000)]
        [Display(Name = "Конски сили")]
        public int HorsePower { get; set; }

        [Required]
        [Display(Name = "Вид гориво")]
        public string FuelType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Скоростна кутия")]
        public string Transmission { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Тип обява")]
        public ListingType Type { get; set; }
        public OfficeLocation CurrentOffice { get; set; }

        [Range(1, 1000000)]
        [Display(Name = "Цена за продажба")]
        public decimal? SalePrice { get; set; }

        [Range(1, 10000)]
        [Display(Name = "Цена за наем (на ден)")]
        public decimal? RentPricePerDay { get; set; }

        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        [Required]
        [Display(Name = "Статус")]
        public StatusType Status { get; set; } = StatusType.Available;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == ListingType.ForSale)
            {
                if (SalePrice == null)
                {
                    yield return new ValidationResult(
                        "Цената за продажба е задължителна.",
                        new[] { nameof(SalePrice) });
                }

                if (RentPricePerDay != null)
                {
                    yield return new ValidationResult(
                        "Колата за продажба не може да има цена за наем.",
                        new[] { nameof(RentPricePerDay) });
                }
            }

            if (Type == ListingType.ForRent)
            {
                if (RentPricePerDay == null)
                {
                    yield return new ValidationResult(
                        "Цената за наем е задължителна.",
                        new[] { nameof(RentPricePerDay) });
                }

                if (SalePrice != null)
                {
                    yield return new ValidationResult(
                        "Колата за наем не може да има цена за продажба.",
                        new[] { nameof(SalePrice) });
                }
            }
        }
    }
}
