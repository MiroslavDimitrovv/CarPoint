using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models.ViewModels
{
    public class CarValuationVm
    {
        [Display(Name = "Марка")]
        [Required(ErrorMessage = "Моля, въведете марка.")]
        public string Brand { get; set; } = string.Empty;

        [Display(Name = "Модел")]
        [Required(ErrorMessage = "Моля, въведете модел.")]
        public string Model { get; set; } = string.Empty;

        [Display(Name = "Година")]
        [Required(ErrorMessage = "Моля, въведете година.")]
        [Range(1990, 2100, ErrorMessage = "Годината трябва да е между 1990 и 2100.")]
        public int? Year { get; set; }

        [Display(Name = "Километраж (km)")]
        [Required(ErrorMessage = "Моля, въведете километрите.")]
        [Range(0, 1_000_000, ErrorMessage = "Километражът трябва да е между 0 и 1 000 000.")]
        public int? Mileage { get; set; }

        [Display(Name = "Гориво")]
        [Required(ErrorMessage = "Моля, изберете гориво.")]
        public string FuelType { get; set; } = string.Empty;

        [Display(Name = "Скоростна кутия")]
        [Required(ErrorMessage = "Моля, изберете скоростна кутия.")]
        public string Transmission { get; set; } = string.Empty;

        [Display(Name = "Двигател")]
        [Required(ErrorMessage = "Моля, въведете двигател (напр. 1,9 TDI).")]
        [StringLength(50, ErrorMessage = "Двигателят е твърде дълъг.")]
        public string Engine { get; set; } = string.Empty;

        [Display(Name = "Мощност (к.с.)")]
        [Required(ErrorMessage = "Моля, въведете мощност.")]
        [Range(30, 1500, ErrorMessage = "Мощността трябва да е между 30 и 1500 к.с.")]
        public int? HorsePower { get; set; }

        [Display(Name = "Тип купе")]
        [Required(ErrorMessage = "Моля, изберете тип купе.")]
        public string BodyType { get; set; } = string.Empty;

        [Display(Name = "Състояние")]
        [Required(ErrorMessage = "Моля, изберете състояние.")]
        public string Condition { get; set; } = "Good";

        [Display(Name = "Катастрофирала")]
        public bool HadAccident { get; set; } = false;

        [Display(Name = "Брой собственици")]
        [Range(1, 10, ErrorMessage = "Броят собственици трябва да е между 1 и 10.")]
        public int OwnersCount { get; set; } = 1;
        public decimal? EstimatedPrice { get; set; }
        public bool IsCalculated { get; set; }
        public string Currency { get; set; } = "EUR";
        public string? ErrorMessage { get; set; }
    }
}
