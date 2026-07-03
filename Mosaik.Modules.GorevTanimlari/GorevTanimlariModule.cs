using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.GorevTanimlari.Entities;

namespace Mosaik.Modules.GorevTanimlari;

// Görev Tanımları modülü — pozisyon görev-tanımı + Zirve personel eşleme ekranı.
public class GorevTanimlariModule : IMosaikModule
{
    public string ModuleKey => "gorevtanimlari";
    public string DisplayName => "Görev Tanımları";
    public string? Icon => "fas fa-sitemap";
    public int DisplayOrder => 120;
    public string? MigrationFolder => "Database";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Services.MappingService>();
        services.AddScoped<Services.SemaService>();
        services.AddScoped<Services.SemaEditService>();
    }

    public void ConfigureModelBuilder(ModelBuilder mb)
    {
        mb.Entity<GorevDocument>(e =>
        {
            e.ToTable("GorevDocuments");
            e.HasKey(d => d.Id);
            e.Property(d => d.PositionCode).HasMaxLength(200).IsRequired();
            e.Property(d => d.Title).HasMaxLength(300).IsRequired();
            e.HasIndex(d => d.PositionCode).IsUnique();
        });

        mb.Entity<GorevZirveMap>(e =>
        {
            e.ToTable("GorevZirveMap");
            e.HasKey(m => m.Id);
            e.Property(m => m.ZirveDepartman).HasMaxLength(100);
            e.Property(m => m.ZirveUnvan).HasMaxLength(100).IsRequired();
            e.Property(m => m.MatchType).HasMaxLength(10);
            e.HasIndex(m => new { m.ZirveDepartman, m.ZirveUnvan }).IsUnique();
        });

        mb.Entity<GorevPersonelMap>(e =>
        {
            e.ToTable("GorevPersonelMap");
            e.HasKey(m => m.Id);
            e.Property(m => m.Source).HasMaxLength(10);
            e.HasIndex(m => m.Personelno).IsUnique();
        });

        mb.Entity<GorevVersion>(e =>
        {
            e.ToTable("GorevVersions");
            e.HasKey(v => v.Id);
            e.Property(v => v.ContentJson).IsRequired();
            e.HasIndex(v => new { v.GorevDocumentId, v.VersionNumber });
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAreaControllerRoute(
            name: "gorevtanimlari",
            areaName: "GorevTanimlari",
            pattern: "GorevTanimlari/{controller=Mapping}/{action=Index}/{id?}");
    }
}
