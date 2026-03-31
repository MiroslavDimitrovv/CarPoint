using CarPoint.Data;
using CarPoint.Models;
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

        public FavoritesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
