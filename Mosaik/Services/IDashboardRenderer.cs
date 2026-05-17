using Mosaik.Models;

namespace Mosaik.Services
{
    public interface IDashboardRenderer
    {
        string Render(DashboardConfig config, List<List<Dictionary<string, object>>> resultSets);
    }
}
