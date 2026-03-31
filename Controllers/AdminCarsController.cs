using CarPoint.Data;
using CarPoint.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCarsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminCarsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index(string? q, string? type, string? status, string? office)
        {
            var carsQuery = _db.Cars.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                carsQuery = carsQuery.Where(c =>
                    (c.Brand ?? "").Contains(q) ||
                    (c.Model ?? "").Contains(q) ||
                    (c.FuelType ?? "").Contains(q) ||
                    (c.Engine ?? "").Contains(q) ||
                    (c.Transmission ?? "").Contains(q) ||
                    c.Year.ToString().Contains(q) ||
                    c.Mileage.ToString().Contains(q)
                );
            }

            if (!string.IsNullOrWhiteSpace(type) &&
                Enum.TryParse<Car.ListingType>(type, ignoreCase: true, out var listingType))
            {
                carsQuery = carsQuery.Where(c => c.Type == listingType);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<Car.StatusType>(status, ignoreCase: true, out var st))
            {
                carsQuery = carsQuery.Where(c => c.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(office) &&
                Enum.TryParse<OfficeLocation>(office, ignoreCase: true, out var off))
            {
                carsQuery = carsQuery.Where(c => c.CurrentOffice == off);
            }

            var cars = await carsQuery
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(cars);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var car = await _db.Cars.FindAsync(id);
            if (car == null) return NotFound();

            return View(car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Car car, IFormFile? image)
        {
            if (id != car.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(car);

            var dbCar = await _db.Cars.FirstOrDefaultAsync(c => c.Id == id);
            if (dbCar == null) return NotFound();

            dbCar.Brand = car.Brand;
            dbCar.Model = car.Model;
            dbCar.Year = car.Year;
            dbCar.Description = car.Description;
            dbCar.Mileage = car.Mileage;
            dbCar.Engine = car.Engine;
            dbCar.HorsePower = car.HorsePower;
            dbCar.FuelType = car.FuelType;
            dbCar.Transmission = car.Transmission;
            dbCar.Type = car.Type;
            dbCar.SalePrice = car.SalePrice;
            dbCar.RentPricePerDay = car.RentPricePerDay;
            dbCar.Status = car.Status;

            dbCar.CurrentOffice = car.CurrentOffice;

            if (image != null && image.Length > 0)
            {
                DeleteImageIfNotPlaceholder(dbCar.ImageFileName);
                dbCar.ImageFileName = await SaveImageAsync(image);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View(new Car
            {
                Status = Car.StatusType.Available,
                CurrentOffice = OfficeLocation.Ruse
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return View(car);

            if (!Enum.IsDefined(typeof(OfficeLocation), car.CurrentOffice))
                car.CurrentOffice = OfficeLocation.Ruse;

            if (image != null && image.Length > 0)
                car.ImageFileName = await SaveImageAsync(image);
            else
                car.ImageFileName ??= "placeholder.jpg";

            _db.Cars.Add(car);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var car = await _db.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _db.Cars.FindAsync(id);
            if (car == null) return NotFound();

            DeleteImageIfNotPlaceholder(car.ImageFileName);

            _db.Cars.Remove(car);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "images", "cars");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext.ToLower()))
                return "placeholder.jpg";

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        private void DeleteImageIfNotPlaceholder(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "placeholder.jpg")
                return;

            var fullPath = Path.Combine(_env.WebRootPath, "images", "cars", fileName);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
