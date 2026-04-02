using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdminEventLogger _events;

        public FavoritesController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IAdminEventLogger events)
        {
            _db = db;
            _userManager = userManager;
            _events = events;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var favorites = await _db.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Car)
                .OrderByDescending(f => f.Id)
                .ToListAsync();

            var catalogCarIds = favorites
                .Where(f => f.Car != null && (f.Car.SalePrice ?? 0) > 0)
                .Select(f => f.CarId)
                .Distinct()
                .ToList();

            var rentalCarIds = favorites
                .Where(f => f.Car != null && (f.Car.RentPricePerDay ?? 0) > 0)
                .Select(f => f.CarId)
                .Distinct()
                .ToList();

            ViewBag.CatalogCarIds = catalogCarIds;
            ViewBag.RentalCarIds = rentalCarIds;

            return View(favorites);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int carId, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);

            var exists = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CarId == carId);

            var car = await _db.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == carId);

            if (exists == null)
            {
                _db.Favorites.Add(new Favorite
                {
                    UserId = userId!,
                    CarId = carId
                });
            }
            else
            {
                _db.Favorites.Remove(exists);
            }

            await _db.SaveChangesAsync();

            await _events.LogAsync(
                type: exists == null ? "FavoriteAdded" : "FavoriteRemoved",
                title: exists == null ? "Автомобилът е добавен в любими" : "Автомобилът е премахнат от любими",
                details: car == null ? $"CarId={carId}" : $"{car.Brand} {car.Model}",
                targetUserId: userId,
                targetEmail: (await _userManager.GetUserAsync(User))?.Email,
                carId: carId);

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
