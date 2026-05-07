using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;

namespace Mosaik.Modules.Tamim
{
    // Plan 17 Faz A — Tamim modülü iskelet. Faz B yanlış pattern (generic
    // Title+Body) revert edildi (2026-05-08). Doğru pattern Plan 17 v2 ile
    // implement edilecek: GunlukBlok + Tamim zarfı + TamimBlok junction +
    // 17:00 cron derleme.
    public class TamimModule : IMosaikModule
    {
        public string ModuleKey => "tamim";
        public string DisplayName => "Tamim & Sirküler";
        public string? Icon => "fas fa-bullhorn";
        public int DisplayOrder => 100;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            // Plan 17 v2'de doldurulacak (BlokService + TamimService + Hangfire job)
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            // Plan 17 v2'de doldurulacak (GunlukBlok + Tamim + TamimBlok + TamimOkudu)
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
