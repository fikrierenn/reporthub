# ADR-015 · Yeni modüller ayrı assembly + mevcut 4 modül 1/ay tempoda çıkarılır

- **Durum:** Kabul edildi (14 Mayıs 2026 — [VISION.md](../VISION.md) §5 "Module ayrımı tutarsız" işaretine yanıt)
- **Etkilenen:** `Mosaik.Modules.*/` yeni csproj'lar, `Mosaik/Controllers/Documents,Contracts,OrgChart,Calendar` (mevcut 4 modül), `vnext-entity-port` skill, `IMosaikModule` discovery
- **İlgili ADR'ler:** [ADR-002](002-modular-monolith.md) (modüler monolit decomposition — bu ADR onun **enforcement kuralını** netleştirir)

## Bağlam

ADR-002 (2026-05-08) compile-time modüler monolit kararı verdi: her vNext modül `Mosaik.Modules.<X>` ayrı csproj'unda. Tek implement edilen modül: [`Mosaik.Modules.Circular`](../../Mosaik.Modules.Circular/) (Plan 17 Tamim).

Ancak mevcut 4 modül (canlı 2026-05-14):

| Modül | Konum | Boyut |
|---|---|---|
| Documents | `Mosaik/Controllers/DocumentsController.cs` | ana proje |
| Contracts | `Mosaik/Controllers/ContractsController.cs` (577 satır, M-01 split adayı) | ana proje |
| OrgChart | `Mosaik/Controllers/OrgChartController.cs` | ana proje |
| Calendar | `Mosaik/Controllers/CalendarController.cs` (5.4KB) | ana proje |

**Tutarsızlık:** Tamim ayrı csproj, diğer 4 modül ana projede. ADR-002 kararı net değildi — "yeni modüller ayrı" diyordu ama "mevcut nasıl?" cevabı yoktu. VISION §5 bu boşluğu işaretledi.

İki seçenek vardı: (a) her yeni modül kuralını tut + mevcudu serbest bırak (kalıcı tutarsızlık), (b) mevcudu da çıkar (effort + risk).

## Karar

**İki katmanlı kural:**

### 1. Yeni modüller — Mosaik.Modules.<X> zorunlu (sıkı kural)

- Plan 16 vNext roadmap'teki **henüz başlanmamış** modüller (SOP/Plan 34, Comment-Mention/Plan 35, Workflow Designer/Plan 36, Plan 25 contract devam, vs.) **ayrı csproj** olarak doğar.
- Şablon: [`Mosaik.Modules.Circular`](../../Mosaik.Modules.Circular/) (Plan 17). Yeni modül üretim için [`vnext-entity-port`](../../.claude/skills/vnext-entity-port/SKILL.md) skill kullanılır.
- `IMosaikModule` implementasyonu + kendi `Database/` klasörü + RCL view'lar + DI registration self-register.
- **İstisna yok.** Yeni modül ana projeye eklemek **plan onayı + ADR ek** gerektirir.

### 2. Mevcut 4 modül — 1 modül / ay tempoda çıkarılır (yumuşak kural)

Sıralama (VISION değer + bağımlılık):

| # | Modül | Hedef tarih | Effort | Bağımlılık |
|---|---|---|---|---|
| 1 | **OrgChart** → `Mosaik.Modules.OrgChart` | 2026-06-15 | 4-6 saat | Hiç (kendi içinde kapalı) |
| 2 | **Calendar** → `Mosaik.Modules.Calendar` | 2026-07-15 | 6-8 saat | Plan 22 (Holidays) ile birlikte |
| 3 | **Contracts** → `Mosaik.Modules.Contracts` | 2026-08-15 | 16-20 saat | Plan 25 olgunlaşması, çok bağımlı |
| 4 | **Documents** → `Mosaik.Modules.Documents` | 2026-09-15 | 16-24 saat | Plan 27 fazları + Contracts ile cross-modül |

**Sıralama gerekçesi:**
- **OrgChart** en bağımsız + tamamlanmış (Plan 20 done) → ilk pilot.
- **Calendar** orta — Plan 22 ile birlikte yapılırsa "unified event source" kararı (VISION §3) ile aynı sprint'te kapatılır.
- **Contracts** olgun ama büyük (`ContractsController` 577 satır + Obligations + Compliance bağlı). Plan 25 olgunlaşmadan çıkarmak refactor riski.
- **Documents** en son — Plan 27 5 faz devam ederken çıkarmak iki yönde değişiklik yapar.

**Reports + Dashboard core kalır:**
- `ReportsController` + `DashboardController` cross-modül kullanım merkezleri (her modül rapor + dashboard üretebilir). Çekirdek modül olarak `Mosaik/` ana projede kalmaya devam eder.

### 3. Çıkarma checklist'i (her modül için)

1. **Plan oluştur** — `plans/NN-extract-<module>.md` Tier 3 plan zorunlu.
2. **Csproj iskeleti** — `vnext-entity-port` skill ile (Circular pattern).
3. **Controller + Services + Models + Views taşı** — git mv ile history korunur.
4. **Migration dosyaları taşı** — `Mosaik.Modules.<X>/Database/` altına. Migration zincirinde sıralama bozulmasın (prefix disiplini).
5. **IMosaikModule impl** — ModuleKey + DisplayName + Icon + DisplayOrder + ConfigureServices + ConfigureModelBuilder + MapEndpoints + MigrationFolder.
6. **Cross-modül referansları audit** — `Mosaik.Modules.<X>` başka modüle referans **veremez**. Eğer veriyorsa abstraction'ı `Mosaik.Core`'a çıkar (ADR-002 kuralı).
7. **Build + test** — `dotnet test` yeşil (test-discipline.md zorunlu).
8. **Smoke** — preview start + modül route'ları + sidebar entry görünüm.

## Alternatifler

- **(A) Hiç çıkarma — yeni modüller ayrı, mevcut serbest** — Kalıcı tutarsızlık. ADR-002'nin amacı (build izolasyonu, bağımlılık disiplini) yarım kalır. VISION §5 işareti çözülmez. **Red.**
- **(B) Hemen 4'ünü birden çıkar** — 6-8 hafta sürer, vNext kalbi (SOP+Comment+Workflow) gecikir. Risk yüksek (4 modül × refactor riski). **Red.**
- **(C) Yeni modüller ayrı + mevcut 1/ay tempoda (seçilen)** — sıkı kural yeniler için, yumuşak takvim mevcut için. vNext kalbi paralelinde ilerler, risk dağıtılır. **Kabul.**
- **(D) Mosaik.Modules.Circular'ı geri ana projeye al** — modüler monolit kararı (ADR-002) ile çelişir. Plan 16.6 + Plan 17 yatırımı boşa gider. **Red.**

## Sonuçlar

**Olumlu:**
- Yeni modül kuralı net — Plan 34/35/36 SOP+Comment+Workflow Designer ayrı csproj olarak doğar.
- Mevcut tutarsızlık 4 ay içinde çözülür (haftalık 4-6 saat yatırım).
- Risk dağıtılmış — her ay 1 modül, başarısız olursa geri alınabilir.
- VISION §5 işareti çözüldü.

**Olumsuz / dikkat:**
- **vNext kalbi (SOP+Comment+Workflow) + 4 modül çıkarma paralel = 4 ay yoğun.** Her sprint'te 1 vNext modül + 1 extract = realistik mi? Plan 16.6 (Circular) 1 oturumda yapıldı, gerçek mevcut Tamim tek modül. Extract sırasında "EF schema bağlılığı" sürprizleri olabilir.
- **Migration zincir disiplini.** Ana proje migration prefix 00-66 ana sıra. Modüller kendi `Database/` altında 01-N başlar — ana zincirle çakışmaz ama numerik tarih sıralaması karışabilir. `sql-migration-writer` skill bu kuralı uygular.
- **Cross-modül abstraction eksik** — Contracts → Documents reference vermeli (sözleşme eki). Mevcut kod doğrudan bağ kuruyor; çıkarma sırasında `Mosaik.Core`'a `IDocumentService` çıkarmak ek iş.
- **1/ay tempo gevşek.** Sıkışırsa sıralama değişebilir (Calendar önce, OrgChart sonra). ADR revize edilir, ön onay almadan değişmez.

## Uygulama

### Etkilenen dosyalar / rule

`.claude/rules/architecture.md` "Module ayrımı disiplini" bölümü eklenir:

```markdown
## Module ayrımı (ADR-002 + ADR-015)

- **Yeni modüller:** `Mosaik.Modules.<X>` ayrı csproj zorunlu (istisna yok).
- **Mevcut 4 modül:** 1/ay tempoda çıkarılır (sıra: OrgChart → Calendar → Contracts → Documents).
- **Core kalır:** Reports + Dashboard cross-modül kullanım merkezi.
- **Şablon:** Mosaik.Modules.Circular (Plan 17).
- **Skill:** vnext-entity-port.
```

### Yeni plan adayları

- `plans/37-extract-orgchart-module.md` (2026-06)
- `plans/38-extract-calendar-module.md` (2026-07, Plan 22 ile birlikte)
- `plans/39-extract-contracts-module.md` (2026-08)
- `plans/40-extract-documents-module.md` (2026-09)

## Referanslar

- [ADR-002](002-modular-monolith.md) — modüler monolit decomposition (üst kural)
- [VISION.md](../VISION.md) §5 — Module ayrımı tutarsız işareti
- `plans/16.6-module-extension-architecture.md` — Plan 16.6 implementasyon
- `Mosaik.Modules.Circular/CircularModule.cs` — `IMosaikModule` şablon
- `.claude/skills/vnext-entity-port/SKILL.md` — yeni modül üretim skill'i
- `Mosaik.Core/Module/IMosaikModule.cs` + `ModuleLoader.cs`
