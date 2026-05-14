# ADR-002 · Modüler Monolit Decomposition: Mosaik.Core + Mosaik.Modules.* csproj ayrımı

- **Durum:** Kabul edildi (8 Mayıs 2026 — Plan 16.6 onayı; bu ADR retrospektif kayıt 14 Mayıs 2026)
- **Etkilenen:** Mosaik.sln yapısı, `Mosaik.Core/` (yeni csproj), `Mosaik.Modules.Circular/` (ilk modül), `IMosaikModule` + `ModuleLoader`, Plan 17+ tüm vNext modül roadmap'i
- **İlgili plan:** [`plans/16.6-module-extension-architecture.md`](../../plans/16.6-module-extension-architecture.md) (uygulama planı)
- **İlgili ADR'ler:** ADR-001 (data-access — bu kararın altında yatan EF + SP hibrit aynen korunur)

## Bağlam

Plan 16 vNext roadmap **9 modül** planlıyor (Tamim, HR, Doküman, Mesajlaşma, KPI, Audit, Approval, Form/Anket, Sözleşme — Plan 17→26, toplam ~135h). Plan 16.5 shared kit'te başlangıçta **"Mosaik.Core ayrı csproj DEĞİL — alt-namespace yeterli, YAGNI"** kararı verilmişti.

Ancak Plan 16.5 implementasyonu sırasında 5 sinyal ortaya çıktı:

1. **Build süresi büyüyor.** Tek `Mosaik.csproj` her küçük değişiklikte tüm ekosistemi yeniden derliyor. 9 modül + ana host = devasa monolith, IDE yavaşlığı.
2. **Merge conflict yüzeyi genişliyor.** Tek `Program.cs` + tek `MosaikContext` + tek `Database/` klasörü = her modülün eklediği DI/EF/migration aynı dosyada çakışır.
3. **Modüler test izolasyonu yok.** Tamim test'i çalıştırırken HR modülünün entity'leri de yükleniyor. xUnit fixture'larda fan-out büyüyor.
4. **Plan 12 zaten runtime sidebar enable/disable kurdu** (`AppModule` tablosu), ama kod-side hâlâ tek deployment. Çelişki: DB'de "modül kapalı" diyebilirsin ama assembly hâlâ yüklü.
5. **Bağımlılık disiplini yok.** Tamim doğrudan HR namespace'ine reference verirse derleme buna izin verir — yarın HR'ı sökmek istesen Tamim de kırılır. Modüller-arası coupling sessizce büyür.

Kullanıcı 2026-05-08'de net karar verdi: **"tüm modülleri extension gibi yapmak lazım"**. Plan 16.6 (Module Extension Architecture) yazıldı, auto-onay alındı, implementasyon başladı.

## Karar

**Mosaik 3-katmanlı modüler monolit'e geçer:**

1. **`Mosaik.Core/`** — dependency-free shared kernel (sınıf kütüphanesi).
   - `Domain/` — `BaseEntity`, `ServiceResult`, marker interface'ler
   - `Workflow/` — `IWorkflow`, `ApprovalRequest` + `ApprovalStep` + service
   - `Lookup/` — `DictionaryType` + `DictionaryValue` + `LookupService`
   - `DataScope/` — `IUserDataScope`, `DataScopeRegistry` (multi-tenant filter abstraction)
   - `Module/` — `IMosaikModule` interface + `ModuleLoader` assembly tarayıcı
   - `Email/` — `IEmailService`, `EmailSendResult` (Plan 31)
   - `AI/` — `ILlmService`, `FallbackLlmService`, `IAiExtractionService` (Plan 16.5)
   - Bağımlılık: yalnız `Microsoft.EntityFrameworkCore` + `Microsoft.AspNetCore.Mvc.Abstractions`.

2. **`Mosaik/`** — ana host (ASP.NET Core MVC).
   - `Program.cs` → `ModuleLoader.LoadAll(builder.Services, app)` otomatik discovery
   - Legacy core (Reports, Dashboard, Admin, Auth, Profile, Logs, Test) burada kalır
   - `Database/01-66*.sql` legacy core migration'ları burada
   - `MosaikContext` ana DbContext — modüller `ConfigureModelBuilder` ile DbSet ekler

3. **`Mosaik.Modules.<X>/`** — her vNext modül kendi csproj'unda.
   - `<X>Module.cs` (`IMosaikModule` impl) — self-register entry point
   - `Controllers/` (Area route veya custom)
   - `Views/` (Razor Class Library — host'a gömülü)
   - `Models/` (modül entity'leri)
   - `Services/`
   - `Database/` (modüle özel migration'lar, ana host migration zincirine sırayla katılır)

**Csproj referans grafı (sıkı):**
- `Mosaik.Core` ← bağımsız
- `Mosaik` (host) ← Mosaik.Core + tüm `Mosaik.Modules.*`
- `Mosaik.Modules.<X>` ← **yalnız Mosaik.Core** (başka modüle referans **YASAK**)
- `Mosaik.Tests` ← Mosaik (host) + Mosaik.Core

**Cross-modül iletişim kuralı:** Modüller arası doğrudan API çağrısı yok. Tüm cross-modül iletişim `Mosaik.Core`'daki abstraction'lar üzerinden (örn. Tamim → HR personel listesi için `IUserDataScope` registry, Approval için `IApprovalService`, ortak sözlükler için `ILookupService`).

**Sidebar bağlantısı (Plan 12):** `AppModule` tablosuna `AssemblyName` kolonu eklendi. DB-driven enable/disable artık **assembly load**'ı da kontrol eder — kapalı modülün DI servisleri register edilmez.

## Alternatifler

- **(A) Tek csproj, alt-namespace ile organize et** — Plan 16.5'in başlangıç kararı (YAGNI). Reddedildi: 9 modül için build süresi, merge conflict, test izolasyonu, bağımlılık disiplini sinyalleri Plan 16.5 implementasyonu sırasında ortaya çıktı.
- **(B) Microservice'e böl** (her modül ayrı host) — solo dev için ekstrem overkill. Inter-service auth/transport/discovery ek karmaşıklık, deployment maliyeti, dev-loop yavaşlığı. **Red.**
- **(C) Runtime plug-in (MEF / AssemblyLoadContext drop-in DLL)** — modülleri runtime yüklenen DLL'ler olarak tut. Üretimde plug-in eko-sistemi (versiyonlama, izole AssemblyLoadContext, GC riski, debug zorluğu) solo dev için maliyet. **Red — ileri tarihte değerlendirilebilir (Plan 16.6.5 yer tutucu).**
- **(D) Compile-time modüler monolit (seçilen)** — her modül ayrı csproj + `IMosaikModule` self-register + tek deployment. Build/test izolasyonu, bağımlılık disiplini, ileride microservice'e split kolaylığı kazanılır; runtime karmaşıklığı eklenmez. **Kabul.**

## Sonuçlar

**Olumlu:**
- Build izolasyonu: modül değiştirince yalnız ilgili csproj derlenir (incremental).
- Bağımlılık disiplini compiler tarafından enforce edilir — `Mosaik.Modules.Tamim`'in `Mosaik.Modules.HR`'a referans denemesi derleme hatası.
- Test izolasyonu: modül-spesifik test projeleri ileride eklenebilir (şu an `Mosaik.Tests` ana host'u test ediyor).
- Plan 12 runtime enable/disable + Plan 16.6 compile-time decomposition birbirini tamamlar — DB'de kapalı = assembly yüklenmez.
- Yeni modül eklemek mekanik bir iş: `vnext-entity-port` skill'i bu pattern üzerine kuruldu.
- Modül-özel migration klasörü (`Mosaik.Modules.<X>/Database/`) ana host migration zincirini şişirmez.

**Olumsuz / dikkat:**
- **`MosaikContext` hâlâ tek DbContext.** Modüller `ConfigureModelBuilder` ile DbSet ekler ama context paylaşılır → cross-modül transaction kolay, ama modül schema'sını gerçekten izole etmek için ayrı context lazım (gelecek karar, şu an YAGNI).
- **Razor view discovery (RCL):** Modül view'larını host'a göstermek için `Microsoft.NET.Sdk.Razor` SDK + `RazorCompileOnBuild` gerekli. Plan 16.6 implementasyonunda çözüldü ama her yeni modül için tekrar yapılır.
- **Migration sıralaması:** Host migration `01-66` + modül migration `Mosaik.Modules.Circular/Database/01-N` ardışık çakışmaz, ama prefix'lendirme disiplini (her modül kendi serisi) net olmalı. `sql-migration-writer` skill bu kuralı uygular.
- **Geri dönüş orta-zor:** Modülü tekrar host'a inline etmek mümkün ama referans grafını söküp DI'ı düzleştirmek gerekir.
- **Şu an yalnız 1 modül uygulamış** (`Mosaik.Modules.Circular`). Pattern Plan 17 Tamim'de doğru çalıştı, ama Plan 18+ HR/Documents/Calendar implementasyonunda gerçek stress test'i olacak.

## Referanslar

- `plans/16.6-module-extension-architecture.md` — uygulama planı, taşıma adımları
- `Mosaik.Core/Module/IMosaikModule.cs` — interface tanımı
- `Mosaik.Core/Module/ModuleLoader.cs` — assembly tarama + self-register
- `Mosaik.Modules.Circular/Mosaik.Modules.Circular.csproj` — referans implementation
- `Mosaik.Modules.Circular/CircularModule.cs` — `IMosaikModule` impl örneği
- `.claude/skills/vnext-entity-port/SKILL.md` — yeni modül üretim skill'i
- ADR-001 (data-access) — EF + SP hibrit korunur, modül decomposition ortogonal
- Plan 12 `AppModule` runtime enable/disable — kompleman karar
- Plan 16, Plan 16.5 — vNext roadmap + shared kit context
