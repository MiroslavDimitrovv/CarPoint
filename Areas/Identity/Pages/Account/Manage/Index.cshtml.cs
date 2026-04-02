using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarPoint.Data;
using CarPoint.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Display(Name = "Имейл")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Въведете име.")]
            [Display(Name = "Име")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Името трябва да е между {2} и {1} символа.")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Въведете фамилия.")]
            [Display(Name = "Фамилия")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилията трябва да е между {2} и {1} символа.")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Въведете телефон.")]
            [Display(Name = "Телефон")]
            [Phone(ErrorMessage = "Въведете валиден телефонен номер.")]
            public string PhoneNumber { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Email = user.Email ?? "",
                FirstName = client.FirstName,
                LastName = client.LastName,
                PhoneNumber = client.PhoneNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return NotFound();
            }

            client.FirstName = Input.FirstName;
            client.LastName = Input.LastName;
            client.PhoneNumber = Input.PhoneNumber;

            await _context.SaveChangesAsync();

            StatusMessage = "Профилът е обновен успешно.";
            return RedirectToPage();
        }
    }
}
