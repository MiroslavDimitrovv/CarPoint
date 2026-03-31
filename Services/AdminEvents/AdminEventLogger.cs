using CarPoint.Data;
using CarPoint.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace CarPoint.Services.AdminEvents
{
    public class AdminEventLogger : IAdminEventLogger
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminEventLogger(
            ApplicationDbContext db,
            IHttpContextAccessor http,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _http = http;
            _userManager = userManager;
        }

        public async Task LogAsync(
            string type,
            string title,
            string? details = null,
            string? targetUserId = null,
            string? targetEmail = null,
            int? carId = null,
            int? rentalId = null)
        {
            var ctx = _http.HttpContext;

            string? actorUserId = null;
            string? actorEmail = null;

            if (ctx?.User?.Identity?.IsAuthenticated == true)
            {
                actorUserId = _userManager.GetUserId(ctx.User);
                var actor = actorUserId != null ? await _userManager.FindByIdAsync(actorUserId) : null;
                actorEmail = actor?.Email;
            }

            var ev = new AdminEvent
            {
                Type = type,
                Title = title,
                Details = details,

                ActorUserId = actorUserId,
                ActorEmail = actorEmail,

                TargetUserId = targetUserId,
                TargetEmail = targetEmail,

                CarId = carId,
                RentalId = rentalId,

                Ip = ctx?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = ctx?.Request?.Headers["User-Agent"].ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.AdminEvents.Add(ev);
            await _db.SaveChangesAsync();
        }
    }
}
