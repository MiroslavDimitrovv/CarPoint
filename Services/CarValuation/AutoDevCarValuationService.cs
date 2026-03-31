using System.Net.Http.Headers;
using System.Text.Json;

namespace CarPoint.Services.CarValuation
{
    public class AutoDevCarValuationService : ICarValuationService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public AutoDevCarValuationService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<CarValuationResult> GetValuationAsync(CarValuationRequest request, CancellationToken ct = default)
        {
            var apiKey = _config["AutoDev:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Липсва AutoDev:ApiKey в appsettings.json");

            var limit = 50;
            if (int.TryParse(_config["AutoDev:Limit"], out var parsed) && parsed is >= 5 and <= 100)
                limit = parsed;

            var strictPrices = await QueryPricesAsync(apiKey,
                make: request.Brand,
                model: request.Model,
                year: request.Year,
                mileageMin: (int)Math.Round(request.Mileage * 0.8),
                mileageMax: (int)Math.Round(request.Mileage * 1.2),
                limit: limit,
                ct: ct);

            var wideMileagePrices = strictPrices.Count >= 5
                ? strictPrices
                : await QueryPricesAsync(apiKey,
                    make: request.Brand,
                    model: request.Model,
                    year: request.Year,
                    mileageMin: (int)Math.Round(request.Mileage * 0.5),
                    mileageMax: (int)Math.Round(request.Mileage * 1.5),
                    limit: limit,
                    ct: ct);

            var yearOnlyPrices = wideMileagePrices.Count >= 5
                ? wideMileagePrices
                : await QueryPricesAsync(apiKey,
                    make: request.Brand,
                    model: request.Model,
                    year: request.Year,
                    mileageMin: null,
                    mileageMax: null,
                    limit: limit,
                    ct: ct);

            var loosePrices = yearOnlyPrices.Count >= 5
                ? yearOnlyPrices
                : await QueryPricesAsync(apiKey,
                    make: request.Brand,
                    model: request.Model,
                    year: null,
                    mileageMin: null,
                    mileageMax: null,
                    limit: limit,
                    ct: ct);

            var prices = loosePrices;

            if (prices.Count == 0)
                throw new InvalidOperationException("Не намерихме сравними обяви за тази комбинация марка/модел.");

            prices.Sort();
            var median = (prices.Count % 2 == 1)
                ? prices[prices.Count / 2]
                : (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]) / 2m;

            if (request.HadAccident) median *= 0.90m;

            median *= request.Condition switch
            {
                "Excellent" => 1.05m,
                "Good" => 1.00m,
                "Average" => 0.93m,
                "Poor" => 0.85m,
                _ => 1.00m
            };

            median = Math.Round(median, 0);

            return new CarValuationResult
            {
                EstimatedPrice = median,
                Currency = "EUR",
                Provider = "auto.dev (median listings)"
            };
        }

        private async Task<List<decimal>> QueryPricesAsync(
            string apiKey,
            string make,
            string model,
            int? year,
            int? mileageMin,
            int? mileageMax,
            int limit,
            CancellationToken ct)
        {
            var qs = new List<string>
            {
                "vehicle.make=" + Uri.EscapeDataString(make),
                "vehicle.model=" + Uri.EscapeDataString(model),
                "limit=" + limit
            };

            if (year.HasValue)
                qs.Add("vehicle.year=" + year.Value);

            if (mileageMin.HasValue && mileageMax.HasValue)
                qs.Add("vehicle.mileage=" + Uri.EscapeDataString($"{Math.Max(0, mileageMin.Value)}-{Math.Max(0, mileageMax.Value)}"));

            var url = "https://api.auto.dev/listings?" + string.Join("&", qs);

            using var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await _http.SendAsync(msg, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return new List<decimal>();

            using var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                return new List<decimal>();

            var prices = new List<decimal>();

            foreach (var item in dataEl.EnumerateArray())
            {
                if (item.TryGetProperty("retailListing", out var retail) &&
                    retail.ValueKind == JsonValueKind.Object &&
                    retail.TryGetProperty("price", out var priceEl) &&
                    priceEl.ValueKind == JsonValueKind.Number &&
                    priceEl.TryGetDecimal(out var p) &&
                    p > 0)
                {
                    prices.Add(p);
                }
            }

            return prices;
        }
    }
}
