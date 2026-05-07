using Hangfire.Dashboard;

namespace Mosaik.Services
{
    // Plan 17 v2 — Hangfire dashboard sadece admin rolü erişebilsin.
    // Default Hangfire dashboard auth: localhost-only (production'da bu yetersiz).
    public class HangfireAdminOnlyAuth : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            return http.User?.Identity?.IsAuthenticated == true
                   && http.User.IsInRole("admin");
        }
    }
}
