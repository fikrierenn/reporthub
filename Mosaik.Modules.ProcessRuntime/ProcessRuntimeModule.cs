using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core.Module;
using Mosaik.Modules.ProcessRuntime.Entities;

namespace Mosaik.Modules.ProcessRuntime
{
    // Plan 42 REV 3 Faz 0 (2026-07-05) — Process Execution Runtime walking-skeleton.
    // Council §4.5: ProcessInstance = İNCE vaka konteyneri; TEK yürütme motoru WorkflowEngine.
    // Bu modül YÜRÜTMEZ — vaka açar (workflow'u IWorkflowService ile başlatır), korele eder
    // (aspects), kaba-status'u tek-yön projeksiyonla izler, timeline'ı READ olarak birleştirir.
    public class ProcessRuntimeModule : IMosaikModule
    {
        public string ModuleKey => "process";
        public string DisplayName => "Süreçler";
        public string? Icon => "fas fa-diagram-project";
        public int DisplayOrder => 260;
        public string? MigrationFolder => "Database";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<Services.ProcessExecutionService>();
            services.AddScoped<Services.ProcessInstanceQueryService>();
        }

        public void ConfigureModelBuilder(ModelBuilder mb)
        {
            mb.Entity<ProcessInstance>(e =>
            {
                e.ToTable("ProcessInstances");
                e.HasKey(x => x.Id);
                e.Property(x => x.InstanceCode).HasMaxLength(40).IsRequired();
                e.Property(x => x.Title).HasMaxLength(300).IsRequired();
                e.Property(x => x.InitiatorEmail).HasMaxLength(200);
                e.Property(x => x.ResultCode).HasMaxLength(40);
                e.Property(x => x.SourceTrigger).HasMaxLength(40).IsRequired();
                e.HasIndex(x => x.InstanceCode).IsUnique();
                e.HasIndex(x => new { x.FirmaId, x.Status });
                e.HasIndex(x => x.WorkflowInstanceId);
            });

            mb.Entity<ProcessInstanceAspect>(e =>
            {
                e.ToTable("ProcessInstanceAspects");
                e.HasKey(x => x.Id);
                e.Property(x => x.RelationLabel).HasMaxLength(80);
                e.HasOne(x => x.ProcessInstance)
                    .WithMany(p => p.Aspects)
                    .HasForeignKey(x => x.ProcessInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.ProcessInstanceId, x.AspectType });
                e.HasIndex(x => new { x.AspectType, x.AspectId });
            });
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAreaControllerRoute(
                name: "process",
                areaName: "Process",
                pattern: "Process/{controller=Instance}/{action=Index}/{id?}");
        }
    }
}
