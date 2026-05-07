using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.Tamim.Models;
using Mosaik.Modules.Tamim.Services;

namespace Mosaik.Modules.Tamim
{
    // Plan 17 Faz B — Tamim modülü. Faz A iskelet üzerine entity + service + CRUD.
    public class TamimModule : IMosaikModule
    {
        public string ModuleKey => "tamim";
        public string DisplayName => "Tamim & Sirküler";
        public string? Icon => "fas fa-bullhorn";
        public int DisplayOrder => 100;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<TamimService>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            mb.Entity<Models.Tamim>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Title).HasMaxLength(200).IsRequired();
                e.Property(t => t.Body).IsRequired();
                e.Property(t => t.Status).HasConversion<int>();
                // CreatedAt DEFAULT migration script'inde tanımlı (GETUTCDATE())
                e.HasIndex(t => t.Status);
                e.HasIndex(t => new { t.Status, t.PublishDate });
                e.HasIndex(t => t.CreatedById);
            });

            mb.Entity<TamimReadLog>(e =>
            {
                e.HasKey(r => r.Id);
                // CreatedAt + ReadAt DEFAULT migration script'inde
                e.HasIndex(r => new { r.TamimId, r.UserId }).IsUnique();
                e.HasIndex(r => r.TamimId);
            });
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
