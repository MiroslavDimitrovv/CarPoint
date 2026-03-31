using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarPoint.Controllers
{
    [Authorize]
    public class RentalsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAdminEventLogger _events;

        public RentalsController(ApplicationDbContext db, IAdminEventLogger events)
        {
            _db = db;
            _events = events;
        }

        public async Task<IActionResult> My()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var rentals = await _db.Rentals.AsNoTracking()
                .Include(r => r.Car)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return View(rentals);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var rental = await _db.Rentals
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (rental == null) return NotFound();

            var todayUtc = DateTime.UtcNow.Date;

            if (rental.StartDate.Date <= todayUtc)
            {
                TempData["Error"] = "Не можеш да отмениш наем, който вече е започнал.";
                return RedirectToAction(nameof(My));
            }

            rental.Status = Rental.RentalStatus.Cancelled;
            await _db.SaveChangesAsync();

            await _events.LogAsync(
    "RentalCancelled",
    "Отказан наем",
    $"RentalId={rental.Id}",
    targetUserId: userId,
    carId: rental.CarId,
    rentalId: rental.Id
);


            TempData["Success"] = "Наемът беше отменен.";
            return RedirectToAction(nameof(My));
        }
    }
}
