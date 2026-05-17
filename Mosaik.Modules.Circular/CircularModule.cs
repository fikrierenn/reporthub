using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular
{
    public class CircularModule : IMosaikModule
    {
        public string ModuleKey => "circular";
        public string DisplayName => "Tamim & Sirküler"; // UI etiketi (TR), kod identifier'ı circular
        public string? Icon => "fas fa-bullhorn";
        public int DisplayOrder => 100;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<Services.BlockService>();
            services.AddScoped<Services.CircularService>();
            services.AddScoped<Services.BlockFileService>();
            services.AddScoped<Services.CompileCircularJob>();
            services.AddScoped<Services.TamimReminderJob>();
            services.AddScoped<Services.CircularSummaryService>();
            // Plan 17 Faz D + C-03 — RecurringJob register Mosaik/Program.cs'de
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            mb.Entity<DailyBlock>(e =>
            {
                e.HasKey(b => b.Id);
                e.Property(b => b.BlockNumber).HasMaxLength(50).IsRequired();
                e.Property(b => b.Department).HasMaxLength(100).IsRequired();
                e.Property(b => b.Subject).HasMaxLength(200).IsRequired();
                e.Property(b => b.Content).IsRequired();
                e.HasIndex(b => b.BlockNumber).IsUnique();
                e.HasIndex(b => new { b.BlockDate, b.CircularId });  // bekleyen vs yayında
                e.HasIndex(b => b.CreatedById);
                e.HasIndex(b => b.BlockTypeId);
                e.HasIndex(b => b.CircularId);
            });

            mb.Entity<Models.Circular>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.CircularNumber).HasMaxLength(50).IsRequired();
                e.Property(t => t.Title).HasMaxLength(200).IsRequired();
                e.HasIndex(t => t.CircularNumber).IsUnique();
                e.HasIndex(t => t.CircularDate);
                e.HasIndex(t => t.PublishedAt);
                // AiSummaryJson NVARCHAR(MAX), AiSummaryAt nullable — DB default
            });

            mb.Entity<BlockFile>(e =>
            {
                e.HasKey(d => d.Id);
                e.Property(d => d.FileName).HasMaxLength(255).IsRequired();
                e.Property(d => d.FilePath).HasMaxLength(500).IsRequired();
                e.Property(d => d.Extension).HasMaxLength(10).IsRequired();
                e.Property(d => d.MimeType).HasMaxLength(100);
                e.HasIndex(d => d.BlockId);
                e.HasIndex(d => new { d.BlockId, d.IsActive });
            });
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAreaControllerRoute(
                name: "circular",
                areaName: "Circular",
                pattern: "Circular/{controller=Home}/{action=Index}/{id?}");
        }
    }
}
