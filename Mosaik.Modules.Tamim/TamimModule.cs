using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.Tamim.Models;

namespace Mosaik.Modules.Tamim
{
    public class TamimModule : IMosaikModule
    {
        public string ModuleKey => "tamim";
        public string DisplayName => "Tamim & Sirküler";
        public string? Icon => "fas fa-bullhorn";
        public int DisplayOrder => 100;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            // Plan 17 Faz C'de service'ler eklenecek (BlokService + TamimService)
            // Plan 17 Faz D'de Hangfire RecurringJob + TamimDerleyiciJob
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            mb.Entity<GunlukBlok>(e =>
            {
                e.HasKey(b => b.Id);
                e.Property(b => b.BlokNo).HasMaxLength(50).IsRequired();
                e.Property(b => b.DepartmanAdi).HasMaxLength(100).IsRequired();
                e.Property(b => b.Konu).HasMaxLength(200).IsRequired();
                e.Property(b => b.Aciklama).IsRequired();
                e.Property(b => b.RedSebebi).HasMaxLength(500);
                e.Property(b => b.Durum).HasConversion<int>();
                e.HasIndex(b => b.BlokNo).IsUnique();
                e.HasIndex(b => new { b.BlokTarihi, b.Durum });
                e.HasIndex(b => b.OlusturanId);
                e.HasIndex(b => b.BlokTuruId);
            });

            mb.Entity<Models.Tamim>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.TamimNo).HasMaxLength(50).IsRequired();
                e.Property(t => t.Baslik).HasMaxLength(200).IsRequired();
                e.HasIndex(t => t.TamimNo).IsUnique();
                e.HasIndex(t => t.TamimTarihi);
                e.HasIndex(t => t.YayinTarihi);
            });

            mb.Entity<TamimBlok>(e =>
            {
                e.HasKey(tb => tb.Id);
                e.HasIndex(tb => new { tb.TamimId, tb.BlokId }).IsUnique();
                e.HasIndex(tb => tb.TamimId);
                e.HasIndex(tb => tb.BlokId);
            });

            mb.Entity<TamimOkudu>(e =>
            {
                e.HasKey(o => o.Id);
                e.HasIndex(o => new { o.TamimId, o.UserId }).IsUnique();
                e.HasIndex(o => o.UserId);
                e.HasIndex(o => o.TamimId);
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
