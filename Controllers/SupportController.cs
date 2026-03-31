using System.Security.Claims;
using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Models.ViewModels.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SupportController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string GuestSupportEmail = "guest-support@carpoint.local";
        private const string GuestNamePrefix = "Име за контакт:";
        private const string GuestEmailPrefix = "Имейл за контакт:";

        public SupportController(
            ApplicationDbContext db,
            ILogger<SupportController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View(new SupportTicketCreateVm());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicketCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var signedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isGuest = string.IsNullOrWhiteSpace(signedInUserId);
            var ticketOwnerId = signedInUserId;
            var rawDescription = vm.Description.Trim();

            if (isGuest)
            {
                if (string.IsNullOrWhiteSpace(vm.GuestName))
                {
                    ModelState.AddModelError(nameof(vm.GuestName), "Въведи име.");
                }

                if (string.IsNullOrWhiteSpace(vm.GuestEmail))
                {
                    ModelState.AddModelError(nameof(vm.GuestEmail), "Въведи имейл.");
                }

                if (!ModelState.IsValid)
                {
                    return View(vm);
                }

                var trimmedGuestEmail = vm.GuestEmail!.Trim();
                var existingUser = await _userManager.FindByEmailAsync(trimmedGuestEmail);

                if (existingUser != null)
                {
                    ticketOwnerId = existingUser.Id;
                }
                else
                {
                    var guestUser = await EnsureGuestSupportUserAsync();
                    ticketOwnerId = guestUser.Id;
                }

                rawDescription = BuildGuestDescription(vm.GuestName!, trimmedGuestEmail, rawDescription);
            }

            var ticket = new SupportTicket
            {
                UserId = ticketOwnerId!,
                Subject = vm.Subject.Trim(),
                Description = rawDescription,
                Category = vm.Category,
                Priority = vm.Priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync();

            var message = new SupportTicketMessage
            {
                TicketId = ticket.Id,
                AuthorUserId = ticketOwnerId!,
                IsAdmin = false,
                Message = rawDescription,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTicketMessages.Add(message);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "SupportTicket CREATED: TicketId={TicketId} UserId={UserId} Subject={Subject} At={AtUtc}",
                ticket.Id,
                ticketOwnerId,
                ticket.Subject,
                ticket.CreatedAt);

            TempData["Success"] = $"Заявката е изпратена успешно. №{ticket.Id}";

            if (isGuest)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(My));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> My()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = (await _userManager.GetUserAsync(User))?.Email;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var guestUser = await _userManager.FindByEmailAsync(GuestSupportEmail);
            var guestUserId = guestUser?.Id;

            var tickets = await _db.SupportTickets
                .AsNoTracking()
                .Where(t =>
                    t.UserId == userId ||
                    (!string.IsNullOrWhiteSpace(userEmail) &&
                     guestUserId != null &&
                     t.UserId == guestUserId &&
                     t.Description.Contains($"{GuestEmailPrefix} {userEmail}")))
                .OrderByDescending(t => t.Id)
                .Select(t => new SupportTicketRowVm
                {
                    Id = t.Id,
                    CreatedAt = t.CreatedAt,
                    Subject = t.Subject,
                    Category = t.Category,
                    Priority = t.Priority,
                    Status = t.Status
                })
                .ToListAsync();

            return View(tickets);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _db.SupportTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            var messages = await _db.SupportTicketMessages
                .AsNoTracking()
                .Where(m => m.TicketId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Messages = messages;
            ViewBag.GuestName = ExtractGuestField(ticket.Description, GuestNamePrefix);
            ViewBag.GuestEmail = ExtractGuestField(ticket.Description, GuestEmailPrefix);

            return View(ticket);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string message)
        {
            message = (message ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Съобщението не може да е празно.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanAccessTicketAsync(ticket))
            {
                return Forbid();
            }

            if (ticket.Status == TicketStatus.Closed)
            {
                TempData["Error"] = "Тази заявка е затворена и не може да се изпращат нови съобщения.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var reply = new SupportTicketMessage
            {
                TicketId = id,
                AuthorUserId = userId,
                IsAdmin = false,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTicketMessages.Add(reply);
            ticket.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "SupportTicket USER_REPLY: TicketId={TicketId} UserId={UserId} At={AtUtc} MsgLen={Len}",
                id,
                userId,
                reply.CreatedAt,
                reply.Message.Length);

            TempData["Success"] = "Съобщението е изпратено.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<bool> CanAccessTicketAsync(SupportTicket ticket)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = (await _userManager.GetUserAsync(User))?.Email;
            var guestUser = await _userManager.FindByEmailAsync(GuestSupportEmail);

            if (!string.IsNullOrWhiteSpace(userId) && ticket.UserId == userId)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(userEmail) &&
                guestUser != null &&
                ticket.UserId == guestUser.Id &&
                ticket.Description.Contains($"{GuestEmailPrefix} {userEmail}"))
            {
                return true;
            }

            return false;
        }

        private async Task<ApplicationUser> EnsureGuestSupportUserAsync()
        {
            var existingUser = await _userManager.FindByEmailAsync(GuestSupportEmail);
            if (existingUser != null)
            {
                return existingUser;
            }

            var guestUser = new ApplicationUser
            {
                UserName = GuestSupportEmail,
                Email = GuestSupportEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(guestUser, "GuestSupport!123");
            if (!result.Succeeded)
            {
                var errorText = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Guest support user could not be created: {errorText}");
            }

            return guestUser;
        }

        private static string BuildGuestDescription(string guestName, string guestEmail, string description)
        {
            return
                $"{GuestNamePrefix} {guestName.Trim()}{Environment.NewLine}" +
                $"{GuestEmailPrefix} {guestEmail.Trim()}{Environment.NewLine}{Environment.NewLine}" +
                description;
        }

        private static string? ExtractGuestField(string description, string prefix)
        {
            var line = description
                .Split(Environment.NewLine)
                .FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return line == null ? null : line[prefix.Length..].Trim();
        }
    }
}
