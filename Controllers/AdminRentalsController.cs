using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRentalsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAdminEventLogger _events;

        public AdminRentalsController(ApplicationDbContext db, IAdminEventLogger events)
        {
            _db = db;
            _events = events;
        }

        public async Task<IActionResult> Occupancy()
        {
            var cars = await _db.Cars
                .Where(c => c.Type == Car.ListingType.ForRent)
                .Include(c => c.Rentals.Where(r => r.Status == Rental.RentalStatus.Active))
                    .ThenInclude(r => r.Client)
                .AsNoTracking()
                .OrderBy(c => c.Brand)
                .ThenBy(c => c.Model)
                .ToListAsync();

            return View(cars);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int rentalId)
        {
            var rental = await _db.Rentals
                .Include(r => r.Client)
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null) return NotFound();

            if (rental.Status != Rental.RentalStatus.Active)
            {
                TempData["Error"] = "Може да маркираш платено само за активен наем.";
                return RedirectToAction(nameof(Occupancy));
            }

            if (rental.PayMethod != Rental.PaymentMethod.CashOnPickup)
            {
                TempData["Error"] = "Този наем не е 'в брой'.";
                return RedirectToAction(nameof(Occupancy));
            }

            if (rental.IsPaid)
            {
                TempData["Success"] = "Наемът вече е маркиран като платен.";
                return RedirectToAction(nameof(Occupancy));
            }

            rental.IsPaid = true;
            rental.PaidAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _events.LogAsync(
                type: "PaymentMarkedPaid",
                title: "Маркирано плащане като платено",
                details: $"Наем #{rental.Id}, Кола: {rental.Car?.Brand} {rental.Car?.Model}, Сума: {rental.TotalPrice}€",
                targetUserId: rental.UserId,
                targetEmail: rental.Client?.Email,
                carId: rental.CarId,
                rentalId: rental.Id
            );

            TempData["Success"] = "Плащането беше маркирано като платено.";
            return RedirectToAction(nameof(Occupancy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Release(int rentalId)
        {
            var rental = await _db.Rentals
                .Include(r => r.Car)
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null)
                return NotFound();

            if (rental.Status != Rental.RentalStatus.Active)
                return RedirectToAction(nameof(Occupancy));

            rental.Status = Rental.RentalStatus.Completed;

            if (rental.Car != null)
                rental.Car.CurrentOffice = rental.ReturnOffice;

            await _db.SaveChangesAsync();

            await _events.LogAsync(
                type: "RentalReleased",
                title: "Освободен автомобил (приключен наем)",
                details: $"Наем #{rental.Id}, КолаId={rental.CarId}, Нов офис: {rental.ReturnOffice}",
                targetUserId: rental.UserId,
                targetEmail: rental.Client?.Email,
                carId: rental.CarId,
                rentalId: rental.Id
            );

            TempData["Success"] = "Автомобилът беше освободен успешно.";
            return RedirectToAction(nameof(Occupancy));
        }
    }
}
