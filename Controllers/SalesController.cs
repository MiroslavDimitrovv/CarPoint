using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SalesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CarFilterVm filter)
        {
            var baseQuery = _db.Cars.AsNoTracking()
                .Where(c => c.Status == Car.StatusType.Available &&
                            c.Type == Car.ListingType.ForSale);

            filter.Brands = await baseQuery.Select(c => c.Brand).Distinct().OrderBy(x => x).ToListAsync();
            filter.FuelTypes = await baseQuery.Select(c => c.FuelType).Distinct().OrderBy(x => x).ToListAsync();
            filter.Transmissions = await baseQuery.Select(c => c.Transmission).Distinct().OrderBy(x => x).ToListAsync();

            var q = baseQuery;

            if (!string.IsNullOrWhiteSpace(filter.Q))
            {
                var term = filter.Q.Trim();
                q = q.Where(c =>
                    c.Brand.Contains(term) ||
                    c.Model.Contains(term) ||
                    c.Engine.Contains(term) ||
                    c.FuelType.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filter.Brand))
                q = q.Where(c => c.Brand == filter.Brand);

            if (!string.IsNullOrWhiteSpace(filter.FuelType))
                q = q.Where(c => c.FuelType == filter.FuelType);

            if (!string.IsNullOrWhiteSpace(filter.Transmission))
                q = q.Where(c => c.Transmission == filter.Transmission);

            if (filter.YearFrom.HasValue)
                q = q.Where(c => c.Year >= filter.YearFrom.Value);

            if (filter.YearTo.HasValue)
                q = q.Where(c => c.Year <= filter.YearTo.Value);

            if (filter.MileageTo.HasValue)
                q = q.Where(c => c.Mileage <= filter.MileageTo.Value);

            if (filter.HorsePowerFrom.HasValue)
                q = q.Where(c => c.HorsePower >= filter.HorsePowerFrom.Value);

            if (filter.HorsePowerTo.HasValue)
                q = q.Where(c => c.HorsePower <= filter.HorsePowerTo.Value);

            if (filter.PriceFrom.HasValue)
                q = q.Where(c => c.SalePrice != null && c.SalePrice >= filter.PriceFrom.Value);

            if (filter.PriceTo.HasValue)
                q = q.Where(c => c.SalePrice != null && c.SalePrice <= filter.PriceTo.Value);

            var cars = await q
                .OrderByDescending(c => c.Year)
                .ThenBy(c => c.Brand)
                .ToListAsync();

            var pageVm = new SalesIndexVm { Filter = filter, Cars = cars };
            return View(pageVm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var car = await _db.Cars.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id &&
                                         c.Type == Car.ListingType.ForSale);

            if (car == null) return NotFound();
            return View(car);
        }
    }
}
