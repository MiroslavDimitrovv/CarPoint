namespace CarPoint.Models.ViewModels
{
    public class CarFilterVm
    {
        public string? Q { get; set; }

        public string? Brand { get; set; }
        public string? FuelType { get; set; }
        public string? Transmission { get; set; }

        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }

        public int? MileageTo { get; set; }

        public int? HorsePowerFrom { get; set; }
        public int? HorsePowerTo { get; set; }

        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }

        public OfficeLocation? Office { get; set; }

        public List<string> Brands { get; set; } = new();
        public List<string> FuelTypes { get; set; } = new();
        public List<string> Transmissions { get; set; } = new();
    }
}
