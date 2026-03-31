using CarPoint.Data;
using CarPoint.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminEventsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminEventsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? type, DateTime? fromUtc, DateTime? toUtc)
        {
            q ??= "";
            var term = q.Trim();

            var baseQuery = _db.AdminEvents.AsNoTracking();

            var types = await baseQuery
                .Select(e => e.Type)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var query = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(e => e.Type == type);

            if (fromUtc.HasValue)
                query = query.Where(e => e.CreatedAtUtc >= fromUtc.Value);

            if (toUtc.HasValue)
                query = query.Where(e => e.CreatedAtUtc <= toUtc.Value);

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(e =>
                    (e.Title ?? "").Contains(term) ||
                    (e.Details ?? "").Contains(term) ||
                    (e.ActorEmail ?? "").Contains(term) ||
                    (e.TargetEmail ?? "").Contains(term) ||
                    e.Type.Contains(term) ||
                    (e.Ip ?? "").Contains(term));
            }

            var rows = await query
                .OrderByDescending(e => e.Id)
                .Take(500)
                .ToListAsync();

            var vm = new AdminEventsIndexVm
            {
                Q = q,
                Type = type,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Types = types,
                Rows = rows
            };

            return View(vm);
        }
    }
}
