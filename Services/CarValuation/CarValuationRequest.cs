namespace CarPoint.Services.CarValuation
{
    public class CarValuationRequest
    {
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Mileage { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public string Transmission { get; set; } = string.Empty;

        public string Engine { get; set; } = string.Empty;
        public int HorsePower { get; set; }

        public string BodyType { get; set; } = string.Empty;
        public string Condition { get; set; } = "Good";
        public bool HadAccident { get; set; }
        public int OwnersCount { get; set; }
    }
}
