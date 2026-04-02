using System.Globalization;
using System.Security.Claims;
using CarPoint.Data;
using CarPoint.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Services.SupportNotifications
{
    public class SupportNotificationService : ISupportNotificationService
    {
        private const string GuestSupportEmail = "guest-support@carpoint.local";
        private const string GuestEmailPrefix = "Имейл за контакт:";
        private const string SessionKeyPrefix = "support-ticket-seen-";

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SupportNotificationService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> GetUnreadCountAsync(ClaimsPrincipal user)
        {
            var unreadTicketIds = await GetUnreadTicketIdsAsync(user);
            return unreadTicketIds.Count;
        }

        public async Task<HashSet<int>> GetUnreadTicketIdsAsync(ClaimsPrincipal user)
        {
            var userId = _userManager.GetUserId(user);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new HashSet<int>();
            }

            var userEntity = await _userManager.GetUserAsync(user);
            var userEmail = userEntity?.Email;
            var guestUserId = (await _userManager.FindByEmailAsync(GuestSupportEmail))?.Id;

            var tickets = await _db.SupportTickets
                .AsNoTracking()
                .Where(t =>
                    t.UserId == userId ||
                    (!string.IsNullOrWhiteSpace(userEmail) &&
                     guestUserId != null &&
                     t.UserId == guestUserId &&
                     t.Description.Contains($"{GuestEmailPrefix} {userEmail}")))
                .Select(t => new
                {
                    t.Id,
                    LatestAdminMessageAt = _db.SupportTicketMessages
                        .Where(m => m.TicketId == t.Id && m.IsAdmin)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => (DateTime?)m.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return new HashSet<int>();
            }

            var unreadIds = new HashSet<int>();

            foreach (var ticket in tickets)
            {
                if (!ticket.LatestAdminMessageAt.HasValue)
                {
                    continue;
                }

                var seenAt = GetSeenAt(session, ticket.Id);
                if (!seenAt.HasValue || ticket.LatestAdminMessageAt.Value > seenAt.Value)
                {
                    unreadIds.Add(ticket.Id);
                }
            }

            return unreadIds;
        }

        public async Task MarkTicketAsSeenAsync(int ticketId)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return;
            }

            var latestAdminMessageAt = await _db.SupportTicketMessages
                .AsNoTracking()
                .Where(m => m.TicketId == ticketId && m.IsAdmin)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefaultAsync();

            if (!latestAdminMessageAt.HasValue)
            {
                return;
            }

            session.SetString(GetSessionKey(ticketId), latestAdminMessageAt.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        private static DateTime? GetSeenAt(ISession session, int ticketId)
        {
            var raw = session.GetString(GetSessionKey(ticketId));
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string GetSessionKey(int ticketId) => $"{SessionKeyPrefix}{ticketId}";
    }
}
