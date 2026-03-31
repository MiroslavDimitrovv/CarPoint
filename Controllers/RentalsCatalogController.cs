using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Models.ViewModels;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CarPoint.Controllers
{
    public class RentalsCatalogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAdminEventLogger _events;

        public RentalsCatalogController(ApplicationDbContext db, IAdminEventLogger events)
        {
            _db = db;
            _events = events;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CarFilterVm filter)
        {
            var today = DateTime.UtcNow.Date;

            var baseQuery = _db.Cars.AsNoTracking()
                .Where(c => c.Type == Car.ListingType.ForRent &&
                            c.Status == Car.StatusType.Available &&
                            !_db.Rentals.Any(r =>
                                r.CarId == c.Id &&
                                r.Status == Rental.RentalStatus.Active &&
                                r.StartDate <= today &&
                                r.EndDate >= today));

            filter.Brands = await baseQuery.Select(c => c.Brand).Distinct().OrderBy(x => x).ToListAsync();
            filter.FuelTypes = await baseQuery.Select(c => c.FuelType).Distinct().OrderBy(x => x).ToListAsync();
            filter.Transmissions = await baseQuery.Select(c => c.Transmission).Distinct().OrderBy(x => x).ToListAsync();

            var q = baseQuery;

            if (filter.Office.HasValue)
                q = q.Where(c => c.CurrentOffice == filter.Office.Value);

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
                q = q.Where(c => c.RentPricePerDay >= filter.PriceFrom.Value);

            if (filter.PriceTo.HasValue)
                q = q.Where(c => c.RentPricePerDay <= filter.PriceTo.Value);

            var cars = await q
                .OrderByDescending(c => c.Year)
                .ThenBy(c => c.Brand)
                .ToListAsync();

            return View(new RentalsIndexVm
            {
                Filter = filter,
                Cars = cars
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var car = await _db.Cars.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.Type == Car.ListingType.ForRent);

            if (car == null)
                return NotFound();

            ViewBag.Booked = await _db.Rentals.AsNoTracking()
                .Where(r => r.CarId == id && r.Status == Rental.RentalStatus.Active)
                .Select(r => new { start = r.StartDate.Date, end = r.EndDate.Date })
                .OrderBy(r => r.start)
                .ToListAsync();

            return View(car);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(
            int carId,
            DateTime startDate,
            DateTime endDate,
            OfficeLocation pickupOffice,
            OfficeLocation returnOffice,
            string paymentMethod)
        {
            var car = await _db.Cars.FirstOrDefaultAsync(c => c.Id == carId);
            if (car == null)
                return NotFound();

            if (car.Type != Car.ListingType.ForRent || car.Status != Car.StatusType.Available)
            {
                TempData["Error"] = "Автомобилът не е наличен за наем.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            if (car.RentPricePerDay == null)
            {
                TempData["Error"] = "Липсва цена за наем.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            var start = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

            if (start < DateTime.UtcNow.Date || end < start)
            {
                TempData["Error"] = "Невалиден период за наем.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            var days = (end.Date - start.Date).Days + 1;
            if (days < 1 || days > 60)
            {
                TempData["Error"] = "Периодът за наем трябва да е между 1 и 60 дни.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            var overlap = await _db.Rentals.AnyAsync(r =>
                r.CarId == carId &&
                r.Status == Rental.RentalStatus.Active &&
                start <= r.EndDate &&
                end >= r.StartDate);

            if (overlap)
            {
                TempData["Error"] = "Автомобилът е зает за избрания период.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            var prevRental = await _db.Rentals.AsNoTracking()
                .Where(r => r.CarId == carId
                    && r.Status != Rental.RentalStatus.Cancelled
                    && r.EndDate < start)
                .OrderByDescending(r => r.EndDate)
                .FirstOrDefaultAsync();

            var expectedOffice = prevRental != null ? prevRental.ReturnOffice : car.CurrentOffice;

            if (pickupOffice != expectedOffice)
            {
                TempData["Error"] = $"Колата ще бъде в {expectedOffice} към началната дата. Избери правилния офис за вземане.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null || string.IsNullOrWhiteSpace(client.PhoneNumber))
            {
                TempData["Error"] = "Телефонният номер е задължителен за наем.";
                return RedirectToAction(nameof(Details), new { id = carId });
            }

            if (!Enum.TryParse<Rental.PaymentMethod>(paymentMethod, true, out var pm))
                pm = Rental.PaymentMethod.CashOnPickup;

            var isPaid = pm == Rental.PaymentMethod.CardPrepay;

            var rental = new Rental
            {
                CarId = car.Id,
                UserId = userId,
                ClientId = client.Id,

                StartDate = start,
                EndDate = end,
                Days = days,

                PricePerDay = car.RentPricePerDay.Value,
                TotalPrice = car.RentPricePerDay.Value * days,

                Status = Rental.RentalStatus.Active,
                PayMethod = pm,

                PickupOffice = pickupOffice,
                ReturnOffice = returnOffice,

                IsPaid = isPaid,
                PaidAt = isPaid ? DateTime.UtcNow : null
            };

            _db.Rentals.Add(rental);
            await _db.SaveChangesAsync();

            await _events.LogAsync(
    type: "RentalCreated",
    title: "Създаден наем",
    details: $"CarId={car.Id}, {start:yyyy-MM-dd} до {end:yyyy-MM-dd}, Pickup={pickupOffice}, Return={returnOffice}, Total={rental.TotalPrice}€, Pay={pm}, Paid={rental.IsPaid}",
    targetUserId: userId,
    targetEmail: client.Email,
    carId: car.Id,
    rentalId: rental.Id
);


            TempData["Success"] =
                $"Наемът е създаден успешно. Общо: {rental.TotalPrice} €. " +
                $"Плащане: {(pm == Rental.PaymentMethod.CardPrepay ? "с карта" : "в брой")}.";

            return RedirectToAction(nameof(Details), new { id = car.Id });
        }
    }
}
