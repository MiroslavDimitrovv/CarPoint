using CarPoint.Models;

namespace CarPoint.Services.AdminEvents
{
    public interface IAdminEventLogger
    {
        Task LogAsync(
            string type,
            string title,
            string? details = null,
            string? targetUserId = null,
            string? targetEmail = null,
            int? carId = null,
            int? rentalId = null);
    }
}
