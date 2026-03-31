using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Models.ViewModels;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAdminEventLogger _events;

        public AdminUsersController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAdminEventLogger events)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _events = events;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            q ??= "";
            var term = q.Trim();

            var query =
                from u in _db.Users.AsNoTracking()
                join c in _db.Clients.AsNoTracking() on u.Id equals c.UserId into cc
                from c in cc.DefaultIfEmpty()
                select new
                {
                    u.Id,
                    Email = u.Email ?? "",
                    ClientId = (int?)c.Id,
                    FirstName = c != null ? c.FirstName : null,
                    LastName = c != null ? c.LastName : null,
                    Phone = c != null ? c.PhoneNumber : (u.PhoneNumber ?? null),
                    RentalsCount = _db.Rentals.Count(r => r.UserId == u.Id)
                };

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(x =>
                    x.Email.Contains(term) ||
                    (((x.FirstName ?? "") + " " + (x.LastName ?? "")).Contains(term)) ||
                    (x.Phone ?? "").Contains(term)
                );
            }

            var rowsRaw = await query
                .OrderBy(x => x.Email)
                .ToListAsync();

            var rows = new List<AdminUserRowVm>();

            foreach (var x in rowsRaw)
            {
                var user = await _userManager.FindByIdAsync(x.Id);
                var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

                rows.Add(new AdminUserRowVm
                {
                    Id = x.Id,
                    Email = x.Email,
                    ClientId = x.ClientId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    PhoneNumber = x.Phone,
                    RentalsCount = x.RentalsCount,
                    Roles = roles
                });
            }

            ViewBag.Q = q;
            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string userId, string? q)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Невалиден потребител.";
                return RedirectToAction(nameof(Index), new { q });
            }

            var me = _userManager.GetUserId(User);
            if (me == userId)
            {
                TempData["Error"] = "Не можеш да променяш собствената си админ роля.";
                return RedirectToAction(nameof(Index), new { q });
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var createRole = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!createRole.Succeeded)
                {
                    TempData["Error"] = "Неуспешно създаване на роля Admin.";
                    return RedirectToAction(nameof(Index), new { q });
                }
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен.";
                return RedirectToAction(nameof(Index), new { q });
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Не можеш да премахнеш последния администратор.";
                    return RedirectToAction(nameof(Index), new { q });
                }

                var res = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!res.Succeeded)
                {
                    TempData["Error"] = "Грешка при премахване на Admin роля: " +
                                        string.Join("; ", res.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index), new { q });
                }

                await _events.LogAsync(
                    "UserRoleChanged",
                    "Премахната Admin роля",
                    $"Removed Admin from userId={userId}",
                    targetUserId: userId,
                    targetEmail: user.Email
                );

                TempData["Success"] = $"Потребителят {user.Email} вече НЕ е администратор.";
            }
            else
            {
                var res = await _userManager.AddToRoleAsync(user, "Admin");
                if (!res.Succeeded)
                {
                    TempData["Error"] = "Грешка при добавяне на Admin роля: " +
                                        string.Join("; ", res.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index), new { q });
                }

                await _events.LogAsync(
                    "UserRoleChanged",
                    "Добавена Admin роля",
                    $"Added Admin to userId={userId}",
                    targetUserId: userId,
                    targetEmail: user.Email
                );

                TempData["Success"] = $"Потребителят {user.Email} вече е администратор.";
            }

            return RedirectToAction(nameof(Index), new { q });
        }

        [HttpGet]
        public async Task<IActionResult> Rentals(string userId)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);

            var rentals = await _db.Rentals.AsNoTracking()
                .Include(r => r.Car)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Id)
                .Select(r => new AdminUserRentRowVm
                {
                    Id = r.Id,
                    CarId = r.CarId,
                    CarName = (r.Car != null ? (r.Car.Brand + " " + r.Car.Model) : ("CarId=" + r.CarId)),
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    Days = r.Days,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status,
                    PayMethod = r.PayMethod,
                    IsPaid = r.IsPaid,
                    PickupOffice = r.PickupOffice,
                    ReturnOffice = r.ReturnOffice
                })
                .ToListAsync();

            return View(new AdminUserRentalsVm
            {
                UserId = userId,
                Email = user.Email ?? "",
                FirstName = client?.FirstName,
                LastName = client?.LastName,
                PhoneNumber = client?.PhoneNumber,
                Rentals = rentals
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

            var vm = new AdminUserEditVm
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                ClientId = client?.Id,
                FirstName = client?.FirstName ?? "",
                LastName = client?.LastName ?? "",
                PhoneNumber = client?.PhoneNumber ?? (user.PhoneNumber ?? "")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminUserEditVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.UserId);
            if (user == null) return NotFound();

            user.Email = vm.Email;
            user.UserName = vm.Email;

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == vm.UserId);
            if (client == null)
            {
                client = new Client
                {
                    UserId = vm.UserId,
                    Email = vm.Email,
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    PhoneNumber = vm.PhoneNumber
                };
                _db.Clients.Add(client);
            }
            else
            {
                client.Email = vm.Email;
                client.FirstName = vm.FirstName;
                client.LastName = vm.LastName;
                client.PhoneNumber = vm.PhoneNumber;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Данните на потребителя бяха обновени.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            var me = _userManager.GetUserId(User);
            if (me == userId)
            {
                TempData["Error"] = "Не можеш да изтриеш собствения си акаунт.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            await _events.LogAsync(
                "UserDeleted",
                "Изтрит потребител",
                $"Deleted userId={userId}",
                targetUserId: userId,
                targetEmail: user.Email
            );

            var rentals = await _db.Rentals.Where(r => r.UserId == userId).ToListAsync();
            _db.Rentals.RemoveRange(rentals);

            var favs = await _db.Favorites.Where(f => f.UserId == userId).ToListAsync();
            _db.Favorites.RemoveRange(favs);

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null) _db.Clients.Remove(client);

            _db.Users.Remove(user);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Потребителят беше изтрит.";
            return RedirectToAction(nameof(Index));
        }
    }
}
