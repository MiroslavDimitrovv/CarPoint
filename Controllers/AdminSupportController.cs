using System.Security.Claims;
using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Models.ViewModels.Support;
using CarPoint.Services.AdminEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSupportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminSupportController> _logger;
        private readonly IAdminEventLogger _events;

        private const string GuestNamePrefix = "Име за контакт:";
        private const string GuestEmailPrefix = "Имейл за контакт:";

        public AdminSupportController(
            ApplicationDbContext db,
            ILogger<AdminSupportController> logger,
            IAdminEventLogger events)
        {
            _db = db;
            _logger = logger;
            _events = events;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, TicketStatus? status, string? priorityOrder)
        {
            q ??= string.Empty;
            var term = q.Trim();

            var query =
                from t in _db.SupportTickets.AsNoTracking()
                join u in _db.Users.AsNoTracking() on t.UserId equals u.Id
                join c in _db.Clients.AsNoTracking() on t.UserId equals c.UserId into clients
                from c in clients.DefaultIfEmpty()
                select new
                {
                    t.Id,
                    t.CreatedAt,
                    t.Subject,
                    t.Description,
                    t.Category,
                    t.Priority,
                    t.Status,
                    Email = u.Email ?? string.Empty,
                    FullName = c != null ? c.FullName : null,
                    Phone = c != null ? c.PhoneNumber : (u.PhoneNumber ?? null),
                    LastMessageIsFromUser = _db.SupportTicketMessages
                        .Where(m => m.TicketId == t.Id)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => (bool?)!m.IsAdmin)
                        .FirstOrDefault() ?? false
                };

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(x =>
                    x.Subject.Contains(term) ||
                    x.Email.Contains(term) ||
                    (x.FullName ?? string.Empty).Contains(term) ||
                    (x.Phone ?? string.Empty).Contains(term) ||
                    x.Description.Contains(term));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            query = priorityOrder switch
            {
                "high" => query.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Id),
                "low" => query.OrderBy(x => x.Priority).ThenByDescending(x => x.Id),
                _ => query.OrderByDescending(x => x.Id)
            };

            var rows = await query.ToListAsync();

            var resultRows = rows.Select(x => new
            {
                x.Id,
                x.CreatedAt,
                x.Subject,
                x.Category,
                x.Priority,
                x.Status,
                Email = ExtractGuestField(x.Description, GuestEmailPrefix) ?? x.Email,
                FullName = ExtractGuestField(x.Description, GuestNamePrefix) ?? x.FullName,
                x.Phone,
                x.LastMessageIsFromUser
            }).ToList();

            ViewBag.Q = q;
            ViewBag.Status = status;
            ViewBag.PriorityOrder = priorityOrder;

            return View(resultRows);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _db.SupportTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ticket.UserId);
            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == ticket.UserId);
            var messages = await _db.SupportTicketMessages
                .AsNoTracking()
                .Where(m => m.TicketId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Messages = messages;

            var vm = new AdminSupportDetailsVm
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                Email = ExtractGuestField(ticket.Description, GuestEmailPrefix) ?? user?.Email ?? string.Empty,
                FullName = ExtractGuestField(ticket.Description, GuestNamePrefix) ?? client?.FullName,
                PhoneNumber = client?.PhoneNumber,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Category = ticket.Category,
                Priority = ticket.Priority,
                Status = ticket.Status,
                AdminNote = ticket.AdminNote,
                NewStatus = ticket.Status,
                NewPriority = ticket.Priority,
                NewAdminNote = ticket.AdminNote
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AdminSupportDetailsVm vm)
        {
            var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == vm.Id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = vm.NewStatus;
            ticket.Priority = vm.NewPriority;
            ticket.AdminNote = string.IsNullOrWhiteSpace(vm.NewAdminNote) ? null : vm.NewAdminNote.Trim();
            ticket.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _events.LogAsync(
                type: "SupportTicketUpdated",
                title: "Администратор обнови заявка за поддръжка",
                details: $"TicketId={ticket.Id}, Status={ticket.Status}, Priority={ticket.Priority}",
                targetUserId: ticket.UserId);

            TempData["Success"] = "Заявката е обновена.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        [HttpPost]
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

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var reply = new SupportTicketMessage
            {
                TicketId = id,
                AuthorUserId = adminId,
                IsAdmin = true,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTicketMessages.Add(reply);

            if (ticket.Status == TicketStatus.Open)
            {
                ticket.Status = TicketStatus.InProgress;
            }

            ticket.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _events.LogAsync(
                type: "SupportTicketAdminReply",
                title: "Администратор отговори по заявка",
                details: $"TicketId={ticket.Id}, Subject={ticket.Subject}",
                targetUserId: ticket.UserId);

            _logger.LogInformation(
                "SupportTicket REPLY: TicketId={TicketId} AdminId={AdminId} At={AtUtc} MsgLen={Len}",
                id,
                adminId,
                reply.CreatedAt,
                reply.Message.Length);

            TempData["Success"] = "Отговорът е изпратен.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            var messages = await _db.SupportTicketMessages.Where(m => m.TicketId == id).ToListAsync();
            _db.SupportTicketMessages.RemoveRange(messages);
            _db.SupportTickets.Remove(ticket);
            await _db.SaveChangesAsync();

            await _events.LogAsync(
                type: "SupportTicketDeleted",
                title: "Администратор изтри заявка за поддръжка",
                details: $"TicketId={id}, Subject={ticket.Subject}",
                targetUserId: ticket.UserId);

            TempData["Success"] = "Заявката е изтрита.";
            return RedirectToAction(nameof(Index));
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
