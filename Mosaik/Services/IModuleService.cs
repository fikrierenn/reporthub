using Mosaik.Models;

namespace Mosaik.Services
{
    public interface IModuleService
    {
        Task<IReadOnlyList<AppModule>> GetEnabledAsync();
        Task<IReadOnlyList<AppModule>> GetAllAsync();
        bool IsEnabled(string moduleKey);
        // N-2: Boş kayıt = modül herkese açık. Kayıt varsa → listedeki roller erişebilir.
        bool IsAccessibleForRole(string moduleKey, string roleName);
        void Invalidate();
    }
}
