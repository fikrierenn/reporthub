using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;

namespace Mosaik.Modules.Tamim
{
    // Plan 17 Faz A — Tamim modülü iskelet. ModuleLoader bu sınıfı bulur,
    // ConfigureServices + ConfigureModelBuilder + MapEndpoints çağrıları yapılır.
    //
    // Faz A: sadece Index sayfası (placeholder).
    // Faz B+: entity (Tamim, TamimReadLog), CRUD, onay akışı.
    public class TamimModule : IMosaikModule
    {
        public string ModuleKey => "tamim";
        public string DisplayName => "Tamim & Sirküler";
        public string? Icon => "fas fa-bullhorn";
        public int DisplayOrder => 100;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            // Faz B+: services.AddScoped<ITamimService, TamimService>();
        }

        public void ConfigureModelBuilder(ModelBuilder modelBuilder)
        {
            // Faz B+: modelBuilder.Entity<Tamim>(...) + Entity<TamimReadLog>(...)
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAreaControllerRoute(
                name: "tamim",
                areaName: "Tamim",
                pattern: "Tamim/{controller=Home}/{action=Index}/{id?}");
        }
    }
}
