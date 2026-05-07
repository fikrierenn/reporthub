# Plan 17 — Tamim & Sirküler Modülü

**Durum:** Faz A iskelet implement, Faz B+C+D backlog
**Tier:** 3 (yeni modül + DB + UI + iş akışı)
**Tarih:** 2026-05-08
**Tahmini süre:** ~10 saat (4 faz)
**Önkoşul:** Plan 16.5 ✅ + Plan 16.6 ✅ tamamlandı

## 1. Problem

BKM içi tamim & sirküler dağıtımı şu an manuel (e-posta, ilan tahtası). vNext modül roadmap'inde **birinci sırada** — düşük effort + yüksek görünür değer (kullanıcılar günlük olarak kullanır).

Kaynak: `D:/Dev/tamim/` — Next.js 15 + Prisma + SQL Server prototipi. Mosaik'e port edilecek (Reuse 5/5, birebir adaylığı).

## 2. Scope

**Plan 17 kapsamı:**
- `Mosaik.Modules.Tamim` ayrı csproj (Plan 16.6 modüler monolith pattern)
- 3 entity: `Tamim`, `TamimDurum` (lookup veya enum), `TamimReadLog` (kaynakta yok, eklenir)
- CRUD UI: Liste + Detay + Oluştur + Düzenle + Onay
- Tamim oluşturma → onay → yayınlama akışı (Mosaik.Core.Workflow.ApprovalRequest kullanır)
- Okundu kaydı (TamimReadLog) — kim okudu, ne zaman
- Yayın tarihi otomasyonu (Hangfire opsiyonel — şimdilik manuel)

**Kapsam dışı:**
- Otomatik bildirim (e-posta/Slack/SMS) — Plan 17.x veya Plan 18B sonrası
- Mobil app push — out of scope
- Etiket/kategori — Faz D'ye ertelendi
- Ek dosya yükleme — Faz D'ye ertelendi (Plan 19 Doküman ile birleşik)

## 3. Faz Planı

### Faz A — İskelet (~2 saat) ⭐ BU OTURUMDA
- A1. `Mosaik.Modules.Tamim/Mosaik.Modules.Tamim.csproj` (RCL, .NET 10)
- A2. `TamimModule : IMosaikModule` (Plan 16.6 pattern)
- A3. Boş `TamimController.Index` ("Hello Tamim" placeholder)
- A4. `Views/Tamim/Index.cshtml` minimal layout
- A5. Mosaik.csproj ProjectReference + Mosaik.sln'e ekle
- A6. AppModule seed (`tamim` key, ModuleType='extension', AssemblyName='Mosaik.Modules.Tamim')
- A7. ModuleLoader doğrulama: `Mosaik` çalışınca sidebar'da "Tamim" görünmeli
- A8. Build + test (273 yeşil korunmalı)
- A9. Commit

### Faz B — Entity + Migration + CRUD (~3 saat)
- B1. `Models/Tamim.cs` (BaseEntity inherit, Title/Body/Status/PublishDate/CreatedById)
- B2. `Models/TamimReadLog.cs` (UserId + TamimId + ReadAt unique)
- B3. `TamimModule.ConfigureModelBuilder` entity config
- B4. `Database/01_CreateTamimTables.sql` (idempotent, sql-migration-writer skill)
- B5. `ITamimService` + `TamimService` (CRUD + ListAccessible)
- B6. `TamimController` 5 action (Index/Details/Create POST/Edit POST/Delete POST)
- B7. `ViewModels/TamimFormViewModel.cs`
- B8. Views: Index (liste), Details, Create, Edit
- B9. AuditLog (`tamim_create/update/delete/publish` event'leri)
- B10. Unit test (TamimServiceTests, EF InMemory)

### Faz C — Onay akışı + Yayınlama (~3 saat)
- C1. `TamimStatus` enum (Draft, Pending, Approved, Rejected, Published)
- C2. Mosaik.Core.Workflow.ApprovalService entegrasyonu (EntityType="Tamim")
- C3. `TamimController.Approve` + `Reject` action'ları (yetki: ApproverRole)
- C4. Onay tamamlanınca otomatik Status=Published
- C5. Tamim list filter: Draft (sadece editör), Pending (sadece onayçı), Published (herkes)
- C6. Smoke: editor oluşturur → manager onaylar → yayınlanır

### Faz D — Read log + bildirim önizleme (~2 saat)
- D1. `TamimReadLog` insert: kullanıcı Details sayfasına girince (idempotent — UserId+TamimId unique)
- D2. Detail sayfasında "Okudum" buton (manuel ack) — TamimReadLog.IsAcknowledged
- D3. Index sayfasında "X kişi okudu / Y kişi okumadı" göstergesi (admin için)
- D4. Etiket/kategori desteği (Lookup pattern, Mosaik.Core.Lookup)
- D5. Smoke + final commit

## 4. Mimari kararlar

**`Mosaik.Modules.Tamim` bağımsız csproj:**
- ProjectReference: sadece `Mosaik.Core` (entity + interface)
- Mosaik (host)'a referans verilmez (cross-modül izolasyon)
- Migration'lar modül-içi `Database/` klasöründe (host migration sırası bozulmaz)
- Views RCL pattern ile host'a gömülür (`Views/Tamim/...`)

**Entity bağımlılıkları:**
- `Tamim.CreatedById` → Mosaik.Models.User (host) — ⚠️ cross-csproj FK
  - Çözüm: Tamim entity'si User'a navigation property tutmaz, sadece `int CreatedById`
  - Display için Tamim service `_context.Users.Find(id)` ile lookup yapar
  - Aynı pattern: `Tamim.ApprovedById`, `TamimReadLog.UserId`
- `Tamim.Status` enum (string serialize) — Lookup tablosu YOK (Lookup overkill, enum yeter)

**ApprovalRequest entegrasyonu:**
- Tamim oluşturulup onaya gönderildiğinde `ApprovalService.CreateAsync(entityType: "Tamim", entityId: tamimId, steps: [{ApproverRole: "manager"}])`
- Tamim.Status = `Pending` set edilir
- ApprovalRequest.Status değişikliklerini Tamim'e yansıtmak için event veya manuel kontrol (Faz C tasarım)

## 5. Riskler

| Risk | Mitigation |
|---|---|
| **Cross-modül FK** (Tamim → User) | Navigation property yok, sadece int FK. Service layer lookup. |
| **Migration sırası** (modül-içi vs host) | Modül kendi `Database/01_*.sql` numaralandırması — host migration 35'e kadar etkilenmez |
| **AppModule sidebar görünmezse** | Faz A8 smoke test'te kontrol — IsEnabled=1, AssemblyName doğru, ModuleLoader bulduğundan emin |
| **RCL view'lar host'tan görülmüyor** | `<AddRazorSupportForMvc>true</AddRazorSupportForMvc>` csproj'da, ProjectReference yeterli |
| **Tamim.Status enum migration** | string sütun yeterli, EF enum convert otomatik |
| **Faz C ApprovalService karmaşıklığı** | Mosaik.Core.Workflow zaten test edildi, sadece çağrı pattern'i |

## 6. Done Criteria

**Faz A (BU OTURUMDA):**
- [ ] `Mosaik.Modules.Tamim.csproj` build edilir
- [ ] Mosaik.csproj ProjectReference + Mosaik.sln'e eklendi
- [ ] `TamimModule : IMosaikModule` instance ModuleLoader.DiscoverModules() içinde
- [ ] `http://localhost:5197/Tamim` route 200 (placeholder Index sayfası)
- [ ] AppModule seed: `tamim` key var, IsEnabled=1
- [ ] Sidebar'da "Tamim" görünüyor
- [ ] 273 test korundu

**Faz B-D:** ayrı oturum

## 7. Sonraki Adım

Faz A bittikten sonra: **Plan 18A IK Quick Reports** önceliği yüksek (4-6h, hızlı kazanım, modüler değil). Faz B Tamim CRUD ondan sonra.
