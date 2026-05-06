using Mosaik.Models;

namespace Mosaik.Services
{
    public interface IBrandService
    {
        Task<BrandSettings> GetAsync();
        void Invalidate();
    }
}
