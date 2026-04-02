using System.Security.Claims;

namespace CarPoint.Services.SupportNotifications
{
    public interface ISupportNotificationService
    {
        Task<int> GetUnreadCountAsync(ClaimsPrincipal user);
        Task<HashSet<int>> GetUnreadTicketIdsAsync(ClaimsPrincipal user);
        Task MarkTicketAsSeenAsync(int ticketId);
    }
}
