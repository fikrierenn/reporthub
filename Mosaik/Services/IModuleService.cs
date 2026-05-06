using Mosaik.Models;

namespace Mosaik.Services
{
    public interface IModuleService
    {
        Task<IReadOnlyList<AppModule>> GetEnabledAsync();
        Task<IReadOnlyList<AppModule>> GetAllAsync();
        bool IsEnabled(string moduleKey);
        void Invalidate();
    }
}
