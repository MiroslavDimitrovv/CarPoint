using System;

namespace CarPoint.Services.CarValuation
{
    public class CarValuationResult
    {
        public decimal EstimatedPrice { get; set; }
        public string Currency { get; set; } = "EUR";
        public string? Provider { get; set; }
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
