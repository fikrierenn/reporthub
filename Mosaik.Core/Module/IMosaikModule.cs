using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Mosaik.Core.Module
{
    // Plan 16.6 — Modular Monolith. Her vNext modül (Plan 17+) bu interface'i
    // implement eder. ModuleLoader assembly'leri tarayıp self-register yapar.
    //
    // Convention: modüller arası direkt referans yasak. Cross-modül iletişim
    // sadece Mosaik.Core üzerinden (IUserDataScope, ApprovalService,
    // LookupService gibi shared abstraction'lar).
    public interface IMosaikModule
    {
        // Plan 12 AppModule.ModuleKey ile eşleşir (DB-driven enable/disable)
        string ModuleKey { get; }

        // Sidebar/menü için (Plan 12 AppModule.DisplayName)
        string DisplayName { get; }

        // Font Awesome 6 class (örn. "fas fa-bullhorn")
        string? Icon { get; }

        // Sidebar sıralama (küçük üstte)
        int DisplayOrder { get; }

        // DI kayıtları — modülün service'leri
        void ConfigureServices(IServiceCollection services);

        // EF DbSet konfigürasyonu — modülün entity'leri
        // MosaikContext.OnModelCreating bunu çağırır
        void ConfigureModelBuilder(ModelBuilder modelBuilder);

        // Migration script klasörü relatif path (örn. "Database")
        // Modül kendi klasöründeki .sql script'lerini sırayla uygular.
        // null ise bu modül DB schema değişikliği gerektirmez.
        string? MigrationFolder { get; }

        // Endpoint mapping — Areas pattern veya custom route
        // app.MapAreaControllerRoute / endpoints.MapGet vb.
        void MapEndpoints(IEndpointRouteBuilder endpoints);
    }
}
