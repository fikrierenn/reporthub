using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Mosaik.Core.Module
{
    // Plan 16.6 Faz B — IMosaikModule auto-discovery.
    // AppDomain'deki "Mosaik.Modules.*" assembly'lerini tarar, IMosaikModule
    // implementasyonlarını bulup instance üretir.
    //
    // Cache mantığı: ilk çağrıda discovery, sonrası cache (statik).
    // Test için ResetCache() metodu var.
    public static class ModuleLoader
    {
        private static IReadOnlyList<IMosaikModule>? _cached;
        private static readonly object _lock = new();

        public static IReadOnlyList<IMosaikModule> DiscoverModules()
        {
            if (_cached != null) return _cached;

            lock (_lock)
            {
                if (_cached != null) return _cached;

                // Plan 17+ modülleri load edilmemiş olabilir — Mosaik.Modules.*
                // assembly'lerini explicit yükle (ana host bunları references etmiyor olsa bile)
                LoadModuleAssemblies();

                var modules = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => (a.GetName().Name ?? "").StartsWith(
                        "Mosaik.Modules.", StringComparison.Ordinal))
                    .SelectMany(SafeGetTypes)
                    .Where(t => typeof(IMosaikModule).IsAssignableFrom(t)
                             && !t.IsInterface && !t.IsAbstract)
                    .Select(t => Activator.CreateInstance(t) as IMosaikModule)
                    .Where(m => m != null)
                    .Cast<IMosaikModule>()
                    .OrderBy(m => m.DisplayOrder)
                    .ThenBy(m => m.ModuleKey)
                    .ToList();

                _cached = modules;
                return modules;
            }
        }

        // Modül DLL'lerini bin klasöründen explicit yükle
        // (host csproj'u ProjectReference vermese bile çalışsın)
        private static void LoadModuleAssemblies()
        {
            var binDir = AppContext.BaseDirectory;
            if (!Directory.Exists(binDir)) return;

            foreach (var dll in Directory.EnumerateFiles(binDir, "Mosaik.Modules.*.dll"))
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(dll);
                    if (AppDomain.CurrentDomain.GetAssemblies()
                        .Any(a => a.GetName().Name == name))
                        continue;
                    Assembly.LoadFrom(dll);
                }
                catch
                {
                    // Bozuk DLL veya erişim hatası — modülü atla, log host tarafında
                }
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
        }

        public static void RegisterAll(IServiceCollection services)
        {
            foreach (var m in DiscoverModules())
                m.ConfigureServices(services);
        }

        public static void ApplyToModelBuilder(ModelBuilder modelBuilder)
        {
            foreach (var m in DiscoverModules())
                m.ConfigureModelBuilder(modelBuilder);
        }

        public static void MapAll(IEndpointRouteBuilder endpoints)
        {
            foreach (var m in DiscoverModules())
                m.MapEndpoints(endpoints);
        }

        // Test için
        public static void ResetCache()
        {
            lock (_lock) _cached = null;
        }
    }
}
