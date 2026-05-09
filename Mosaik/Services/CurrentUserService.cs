using System.Security.Claims;

namespace Mosaik.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private static readonly IReadOnlyList<int> EmptyFirmaIds = Array.Empty<int>();

        private readonly IHttpContextAccessor _httpContext;

        public CurrentUserService(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }

        private ClaimsPrincipal? Principal => _httpContext.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public int? UserId
        {
            get
            {
                var val = Principal?.FindFirstValue("userId");
                return int.TryParse(val, out var id) ? id : null;
            }
        }

        public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);

        public IReadOnlyList<int> FirmaIds
        {
            get
            {
                var p = Principal;
                if (p is null) return EmptyFirmaIds;
                var list = new List<int>();
                foreach (var c in p.FindAll("firmaId"))
                {
                    // Defense-in-depth: AuthController claim issue ederken fid > 0 zaten zorunlu;
                    // claim okurken de aynı guard. 0/negatif silently kabul edilirse seed/test
                    // verisine yanlış erişim sızabilir.
                    if (int.TryParse(c.Value, out var id) && id > 0) list.Add(id);
                }
                return list;
            }
        }
    }
}
