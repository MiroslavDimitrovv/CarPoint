using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CarPoint.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IAdminEventLogger _events;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IAdminEventLogger events)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _events = events;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Display(Name = "Имейл")]
            [Required(ErrorMessage = "Моля, въведете имейл.")]
            [EmailAddress(ErrorMessage = "Моля, въведете валиден имейл адрес.")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Име")]
            [Required(ErrorMessage = "Моля, въведете име.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Името трябва да е между {2} и {1} символа.")]
            public string FirstName { get; set; } = string.Empty;

            [Display(Name = "Фамилия")]
            [Required(ErrorMessage = "Моля, въведете фамилия.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилията трябва да е между {2} и {1} символа.")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Телефон")]
            [Required(ErrorMessage = "Моля, въведете телефонен номер.")]
            [Phone(ErrorMessage = "Моля, въведете валиден телефонен номер.")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Display(Name = "Парола")]
            [Required(ErrorMessage = "Моля, въведете парола.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Потвърди паролата")]
            [Required(ErrorMessage = "Моля, потвърдете паролата.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Паролите не съвпадат.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            var client = new Client
            {
                UserId = user.Id,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                PhoneNumber = Input.PhoneNumber,
                Email = Input.Email
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            await _events.LogAsync(
                type: "AccountCreated",
                title: "Създаден акаунт",
                details: $"Име: {client.FirstName} {client.LastName}, Тел: {client.PhoneNumber}",
                targetUserId: user.Id,
                targetEmail: user.Email
            );

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }
    }
}
