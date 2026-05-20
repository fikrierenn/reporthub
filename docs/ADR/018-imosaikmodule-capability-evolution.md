# ADR-018 · IMosaikModule capability evrim — opt-in ayrı interface'ler

- **Durum:** Taslak (20 Mayıs 2026)
- **Etkilenen:** `Mosaik.Core/Module/IMosaikModule.cs`, `Mosaik.Core/Module/Capabilities/*` (yeni), her `Mosaik.Modules.<X>/`, Plan 37 (Unified Inbox), Plan 36 (Workflow widget kataloğu), uzun-vade unified search + Backstage benzeri katalog
- **İlgili ADR'ler:** [ADR-002](002-modular-monolith.md), [ADR-015](015-new-modules-separate-assembly.md), [VISION.md §7.7](../VISION.md)

## Bağlam

`Mosaik.Core/Module/IMosaikModule.cs` şu an 8 üye taşıyor (servis kayıt, model builder, migration folder, endpoint mapping, sidebar metadata). Yeni VISION §7 vizyonu modül başına ek **capability** istiyor:

| Capability | Talep eden | Kullanım |
|---|---|---|
| **Inbox provider** | Plan 37 (Unified Action Inbox) | Her modül kendi onay/atama/oku-okunmadı item'larını tek inbox'a düşürür |
| **Widget provider** | Plan 36 Faz E + Dynamic Dashboard (uzun-vade) | Modül kendi dashboard widget tiplerini kayda alır (`IWidgetProvider.GetDefinitions()`) |
| **Catalog entity provider** | Backstage benzeri varlık kataloğu (uzun-vade) | Org Chart, Tamim, Sözleşme, SOP gibi varlıklar tek kataloğa düşer |
| **Search document provider** | Unified search (uzun-vade) | Her modül indexlenecek dokümanlarını üretir |
| **Entity workflow provider** | Plan 36 W-16 (zaten canlı) | Her modül kendi entity'lerine bağlı workflow instance'ları yayar |
| **Notification subject template** | Plan 32 SMTP + Comment/Mention | Modül kendi bildirim metin şablonlarını sağlar |

Eğer hepsi `IMosaikModule` interface'ine eklenirse:

1. **Mevcut 5 modül kırılır** — Circular, Documents, Contracts, OrgChart, Calendar hepsi yeni metodları implement etmek zorunda.
2. **YAGNI ihlali** — Bir modül sadece "inbox" sağlasa bile diğer 5 metodu boş implement eder. `throw new NotImplementedException()` veya `return Enumerable.Empty<>()` her yerde.
3. **Refactor borcu artar** — Kullanıcı net (2026-05-20): *"eskileri refaktör etmek çok zor iş"*. Eski modüller capability eklemese de çalışmaya devam etmeli.
4. **Open/Closed prensibi** — Yeni capability eklenince mevcut interface signature değişiyor → tüm consumer'lar kırılıyor.

İki seçenek vardı:
- **(A)** Monolit IMosaikModule — tüm capability'ler tek interface, default implementations (C# 8 default interface methods).
- **(B)** Opt-in capability interfaces — her capability ayrı interface, `IMosaikModule` minimum core'da kalır.

## Karar

**B — Opt-in capability interfaces.**

```csharp
// Mevcut çekirdek interface DEĞİŞMEZ
public interface IMosaikModule {
    string ModuleKey { get; }
    string DisplayName { get; }
    string? Icon { get; }
    int DisplayOrder { get; }
    void ConfigureServices(IServiceCollection services);
    void ConfigureModelBuilder(ModelBuilder modelBuilder);
    string? MigrationFolder { get; }
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

// Yeni capability'ler ayrı interface'lerde — modül **isteyerek** implement eder
namespace Mosaik.Core.Module.Capabilities;

public interface IInboxProvider {
    Task<IReadOnlyList<InboxItem>> GetItemsForUserAsync(int userId, HashSet<string> roles, CancellationToken ct);
    string ProviderKey { get; }   // "circular" | "documents" | "workflow" | ...
}

public interface IWidgetProvider {
    IEnumerable<WidgetDefinition> GetDefinitions();
}

public interface ISearchDocumentProvider {
    IAsyncEnumerable<SearchDocument> StreamDocumentsAsync(CancellationToken ct);
}

public interface ICatalogEntityProvider {
    IEnumerable<CatalogEntityDescriptor> Describe();
}

public interface IEntityWorkflowProvider {   // ZATEN VAR (commit c5e4c35) — bu şablon
    Task<IReadOnlyList<EntityWorkflowSummary>> GetForEntityAsync(string entityType, int entityId);
}
```

**Discovery pattern:**

`ModuleLoader` veya `Program.cs` startup'ta `IServiceCollection`'a kayıt:

```csharp
foreach (var assembly in loadedModuleAssemblies) {
    foreach (var type in assembly.GetTypes()) {
        if (typeof(IInboxProvider).IsAssignableFrom(type) && !type.IsInterface)
            services.AddSingleton(typeof(IInboxProvider), type);
        if (typeof(IWidgetProvider).IsAssignableFrom(type) && !type.IsInterface)
            services.AddSingleton(typeof(IWidgetProvider), type);
        // ...
    }
}
```

Consumer tarafı `IEnumerable<IInboxProvider>` enjekte eder → tüm modüllerin item'larını toplar:

```csharp
public class UnifiedInboxService {
    private readonly IEnumerable<IInboxProvider> _providers;
    public async Task<IReadOnlyList<InboxItem>> GetAllAsync(int userId, HashSet<string> roles, CancellationToken ct) {
        var tasks = _providers.Select(p => p.GetItemsForUserAsync(userId, roles, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x).OrderByDescending(i => i.CreatedAt).ToList();
    }
}
```

**Mevcut modüller dokunulmaz.** Capability eklemek isteyen modül kendi class'ına ek `: IInboxProvider` koyar, tek metot doldurur. Kayıt otomatik.

## Reddedilen Alternatifler

### A. Monolit IMosaikModule (default interface methods)

```csharp
public interface IMosaikModule {
    // ... mevcut 8 üye ...
    IEnumerable<InboxItem> GetInboxItems() => Enumerable.Empty<InboxItem>();
    IEnumerable<WidgetDefinition> GetWidgets() => Enumerable.Empty<WidgetDefinition>();
    // ...
}
```

**Red gerekçesi:**
- ✗ Single interface 15+ üyeye şişer (LISP — Interface Segregation Principle ihlali)
- ✗ DI consumer `IMosaikModule` enjekte edip `.GetInboxItems()` çağırır → her modülde boş liste döndürse bile loop overhead
- ✗ Discovery type'ı `IInboxProvider` olamaz → `services.GetServices<IMosaikModule>().SelectMany(m => m.GetInboxItems())` zorlanır, capability'ye filtre yok
- ✗ Capability ekleme = interface signature değişimi = breaking change tüm modüllere
- ✗ Default implementation `throw NotSupportedException` deseninde test edilemez

### C. Abstract base class

```csharp
public abstract class MosaikModuleBase : IMosaikModule {
    public virtual IEnumerable<InboxItem> GetInboxItems() => Enumerable.Empty<InboxItem>();
}
```

**Red gerekçesi:** C# tek-base-class kısıtı + mevcut modüller direkt interface implement ediyor (Circular, Documents vb.) — base'e taşımak refactor demek. Karar prensibi (eski modüller dokunulmaz) ihlal edilir.

### D. Attribute-based discovery

```csharp
[ModuleCapability("inbox")] public class CircularInboxProvider { ... }
```

**Red gerekçesi:** Reflection magic, type safety yok, IntelliSense desteklemez, debugger'da görünmez. Interface daha açık.

## Riskler

| Risk | Etki | Önlem |
|---|---|---|
| Discovery startup'ta yavaşlar | Düşük (assembly tarama ~1 saniye) | Tek seferlik startup işi, request hot path değil |
| Modül `IInboxProvider` implement eder ama `services.AddSingleton<IInboxProvider>` kayıtsız kalır | Inbox item'lar görünmez | ModuleLoader'da `assembly.GetTypes()` tarama otomatik kayıt eder — manuel registration zorunlu değil |
| Çok modül × çok capability × DI cycle | Memory leak / DI loop | Singleton kayıt + capability provider stateless olmalı (kural) |
| Birden çok modül aynı `ProviderKey` döndürür | Inbox duplikasyonu | Discovery sırasında key uniqueness assert |
| Default `Enumerable.Empty` döndüren provider performans yer | Yok (LINQ lazy) | Provider sadece relevant case'de implement edilir, gereksiz boş yok |

## Done Criteria

- `Mosaik.Core/Module/Capabilities/` klasörü 5 capability interface (Inbox, Widget, SearchDocument, CatalogEntity, NotificationTemplate)
- `IEntityWorkflowProvider` (zaten var) bu klasöre taşınır (rename refactor)
- `ModuleLoader` veya `Program.cs` capability discovery loop (~30 satır)
- En az **bir capability** canlı kullanımda (Plan 37 İnbox provider Circular + Documents + Workflow için)
- Tüm mevcut 5 modül **dokunulmadan** build geçer
- ADR'de listelenen 5 capability interface signature stabil — sonraki capability eklenince **mevcut interface signature DEĞİŞMEZ**, sadece yeni interface eklenir

## Rollback

Capability interface yanlış tasarlandıysa: yeni interface'i sil, kullanan modülden `: IXxxProvider` kaldır. `IMosaikModule` çekirdek değişmediği için downstream etki yok.

## İlişkili Plan

- **Plan 37** (Unified Action Inbox) — bu ADR'nin ilk somut implementasyonu. `IInboxProvider` interface burada doğar.
- **Plan 36 Faz E** (gelecek) — `IWidgetProvider` dashboard widget tipleri için.
- **Uzun-vade** — `ISearchDocumentProvider` + `ICatalogEntityProvider` (Backstage benzeri katalog) zaman çizelgesinde değil, VISION §7 kuzey yıldızı.

## Kullanıcı Kararı

**2026-05-20:** *"futuristik plan ona göre yazılmalı"*, *"lükse girmeden, yeni yapacaklarımıza dahil edelim, eskileri refaktör etmek çok zor iş"*.

Bu ADR'nin merkez prensibi: **mevcut 5 modül capability eklemek zorunda değil, eklemek istediği gün opt-in.** Eski kod dokunulmaz.

---

**Numara çakışması notu:** Daha önce `TODO.md` ve journal'da bu ADR "ADR-016" diye anılıyordu. Gerçekte ADR-016 Calendar Unified Event Source'a tahsis edildi (2026-05-15 sonrası). Bu ADR **ADR-018** olarak numaralandı. TODO.md ve VISION.md §7.7 referansları güncellenmeli.
