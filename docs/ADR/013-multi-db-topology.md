# ADR-013 · Multi-DB topology + cross-DB identity bridging

- **Durum:** Kabul edildi (retrospektif kayıt 14 Mayıs 2026 — pratik 2025'ten beri uygulanıyor; analiz isteği üzerine belge düzeyine taşındı)
- **Etkilenen:** `DataSources` tablosu, `ReportCatalog.DataSourceKey`, `IStoredProcedureExecutor`, `UserDataFilterInjector`, `IUserDataScope` (Mosaik.Core), Plan 18B HR sync, BKM kurumsal DB'ler (DerinSIS, BKMDATA, BKM_GENEL, EncoreMerkez)
- **İlgili ADR'ler:** ADR-001 (SP + EF hibrit — bu ADR onun "hangi DB" boyutunu netleştirir), Plan 18 (HR sync), Plan 14 (UserDataFilter prod-ready)

## Bağlam

Mosaik kendi metadata DB'sini (`Mosaik`) yönetir + **4 kurumsal kaynak DB**'sinden rapor verisi okur. Bu çoklu DB topolojisi 2025'ten beri uygulanıyor ama belge düzeyinde kayıt yoktu — yeni geliştirici/Claude oturumu bağlamı yeniden inşa etmek zorunda kalıyor. Plan 18B (HR sync) ön gerekliliği olarak bu boşluğun kapatılması gerekiyor.

**Mevcut DB topology (canlı snapshot 2026-05-14):**

| Key | DB | Server | Amaç | Erişim |
|---|---|---|---|---|
| `MAIN` | `Mosaik` | local SQLEXPRESS | Uygulama metadata (user, role, audit, ReportCatalog, UserDataFilter, AppModule, Brand, Circular...) | RW (sa) |
| `DER` | `DerinSISB*` | 192.168.40.201 | DerinSIS satış + envanter + cari | RO (rapor_readonly) |
| `PDKS` | `BKM` (GecoTime) | 192.168.40.201 | Personel devam kontrol — giriş/çıkış | RO (rapor_readonly) |
| `IK` | Zirve | 192.168.40.25\ZRVSQL2008 | Zirve personel — `vw_PersonelDepartman` 3 firma UNION, 4 seviye hiyerarşi, 272 aktif | RO (rapor_readonly) |

**Problem alanları:**

1. **Bağlam kayboluyor.** Yeni rapor eklerken "hangi DB?" sorusu her seferinde yeniden çözülüyor. DataSource'lar `DataSources` tablosunda ama mantıksal kullanım haritası dokümante değildi.
2. **Identity bridging belirsiz.** Mosaik'teki `User` (login için) ile DerinSIS'teki personel ve Zirve'deki çalışan **3 ayrı identity sistemi**. Plan 18B HR sync'ten önce bu eşleştirmenin nasıl yapılacağı ADR seviyesinde belli değildi.
3. **Multi-tenant filter kapsamı belirsiz.** `UserDataFilter` `FilterKey` (örn. `sube`) + `FilterValue` (örn. `123,456`) ile çalışır — ama bu key'ler **hangi DB'de** tanımlı? PDKS'in "şube" tablosu DerinSIS'in "şube"sinden farklı (BKM heterojen şube). Filter bypass riski.
4. **Bağlantı yönetimi disiplini eksik.** Connection string `DataSources.ConnString` kolonunda plain-text. Kim hangi credential ile bağlanır?
5. **Cross-DB JOIN imkansız.** Linked server yok, synonym yok — bilinçli karar mı, accident mı belirsizdi.

## Karar

### 1. DataSource-bazlı bağlantı ayrımı (Linked Server YOK)

Her external DB ayrı bir `DataSource` kaydıyla yönetilir. SP yürütme path'i (`IStoredProcedureExecutor.ExecuteMultipleAsync`) `ReportCatalog.DataSourceKey` → `DataSources.ConnString` lookup ile ilgili connection'a bağlanır.

- **Linked Server eklenmez.** Cross-DB JOIN performansı zayıf, planlanmamış sorgu yolu oluşturur, security boundary zayıflar. Cross-DB veri ihtiyacı varsa: (a) iki ayrı SP, sonuçları C# tarafında merge; (b) HR sync (Plan 18B) ile Mosaik DB'sine kopya getir.
- **Synonym yok.** Aynı gerekçe.

### 2. Identity bridging — `Mosaik.User` canonical, dış sistem `PersonelNo` ile eşlenir

- `Mosaik.User` = login identity (username + PBKDF2 hash + role). Tek source-of-truth.
- Dış DB personeli ile eşleme **Plan 18B HR sync** ile yapılır:
  - `Mosaik.User.ZirvePersonelNo` (UNIQUE) — Zirve İK numarası
  - `Mosaik.User.Lokasyon`, `Sube`, `Departman`, `HireDate`, `Firma` — Zirve'den senkron
  - Hangfire daily job `vw_PersonelDepartman` → `Mosaik.User` upsert
- DerinSIS / PDKS personeli için ayrı eşleme alanları gerekirse modüller eklenir (BKM şube heterojen — her sistemin kendi key'i).
- Bu eşleme **read-only** — Mosaik dış sisteme yazma yapmaz.

### 3. Multi-tenant filter — DataSource-scoped FilterKey

`UserDataFilter` 2-boyutlu olmalı: `(UserId, DataSourceKey, FilterKey, FilterValue)`. BKM şube heterojen olduğu için "şube" key'i `DER` ve `PDKS` ve `IK` DataSource'larında **farklı ID** kullanır.

- `FilterDefinition.DataSourceKey` zorunlu (Plan 14 Faz B).
- `IUserDataScope` (Mosaik.Core) imzası: `(userId, dataSourceKey, filterKey) → IEnumerable<string>` (allowed values).
- Filter bypass koruması: `UserDataFilterInjector` SP execution path'inde **her zaman** çalışır (deny-by-default).
- SP tarafı: `WHERE (@p IS NULL OR col IN (SELECT value FROM STRING_SPLIT(@p, ',')))`.

### 4. Credential yönetimi

- **Read-only kullanıcı:** Her external DB için `rapor_readonly` (veya eşdeğer least-privilege) kullanıcı.
- **ConnString konumu:** `DataSources.ConnString` kolonu (DB içinde, env var değil) — admin GUI üzerinden yönetilir, audit log'a düşer.
- **Risk:** ConnString plain-text. **Hafifletme:** sadece admin (`[Authorize(Roles="admin")]`) okuyabilir, log mask, prod backup encrypted. **İleri iyileştirme:** Azure Key Vault entegrasyonu (henüz YAGNI, solo dev + iç ağ).
- **Local dev:** `appsettings.Development.json` (gitignored) DefaultConnection. Production env var (TODO G-01).

### 5. MCP allowlist disiplini

- `mcp__sqlserver__*` MCP **sadece** BKM kurumsal DB'ler için: master, DerinSIS*, BKMDATA, EncoreMerkez, BKM.
- **Mosaik DB allowlist'te DEĞİL** — uygulama içi DB için MCP yerine `dotnet run` + SSMS/sqlcmd + `sqlcli` (D:/Dev/sqlcli) kullan. Sebep: MCP write yetkisi accidental kötü etki yapabilir, uygulama DB'si EF migration ile yönetilir.
- `mcp__zirve__*` MCP — BKM_GENEL özelinde (Win Auth, read-only).

## Alternatifler

- **(A) Linked Server / Synonym ile cross-DB JOIN** — yazımı kolay görünür ama: optimizer remote stat'lara güvenemez (performans tahmin edilemez), security boundary zayıflar (cross-DB DDL yetki kazara), distributed transaction MSDTC bağımlılığı. **Red.**
- **(B) Tek mega-DB (BKM_GENEL'e Mosaik tabloları taşı)** — security boundary kaybı, kurumsal DB owner BKM IT'nin Mosaik tablolarını yönetmesi gerekir, EF migration kontrolü kaybedilir, geliştirme/test ortamı kurulumu zorlaşır. **Red.**
- **(C) Mikroservis decomposition (her DB bir servis)** — solo dev için aşırı, inter-service auth/transport/discovery yükü, dev-loop yavaşlığı. **Red.**
- **(D) DataSource-bazlı bağlantı + identity bridging via HR sync (seçilen)** — bu ADR. Her DB security boundary'sini korur, identity Mosaik canonical, cross-DB veri ihtiyacı için Plan 18B HR sync (eager copy) kullanılır. **Kabul.**

## Sonuçlar

**Olumlu:**
- Security boundary net — her DB'nin kendi credential'ı, Mosaik DB'sinden başka DB'ye write imkansız.
- Performans öngörülebilir — cross-DB JOIN yok, her sorgu kendi DB'sinde local.
- Identity bridging tek source-of-truth (Mosaik.User) — login + audit + filter aynı user'a bağlı.
- BKM şube heterojenliği `FilterDefinition.DataSourceKey` ile çözülür — filter bypass koruması yapısal.
- MCP allowlist disiplini accidental write'ı engeller.

**Olumsuz / dikkat:**
- **ConnString plain-text DB'de.** Admin-only erişim + audit + encrypted backup ile hafifletildi ama "credential out of source" prensibi tam karşılanmaz. Azure Key Vault adımı bekliyor.
- **HR sync bekliyor (Plan 18B, ~16-24h).** Şu an `Mosaik.User` ile Zirve personel arası eşleme yok — DataFilter `sube` key'i manuel atanmalı. Faz B aktivasyonu kullanıcı tarafından 8 Mayıs'ta durduruldu.
- **3 DB credential maintenance.** Her DB için kullanıcı, şifre rotation, audit gözetimi. Solo dev için iş yükü.
- **DB shape değişimi (DerinSIS / Zirve schema migration) Mosaik raporlarını kırabilir.** Schema versioning yok — kırıldığında SP refactor + test çalıştırma manuel. CI gate yok.
- **Cross-DB report'ta ortak veri ihtiyacı varsa** (örn. Mosaik kullanıcı listesi × DerinSIS satış) HR sync olmadan SP'de yapılamaz. Workaround: C# tarafında merge.

## Yol Haritası

1. **Plan 18B HR sync** (~16-24h, bekleniyor) — `Mosaik.User` Zirve eşlemesi, Hangfire daily job.
2. **Plan 14 Faz B** — `FilterDefinition (sube, IK)` aktivasyonu, 3 OptionsQuery hazır (kullanıcı 8 Mayıs durdurdu).
3. **TODO G-01 (uzun vadeli)** — ConnString → Azure Key Vault (prod) + User Secrets (dev) geçiş.
4. **Plan 16.5 Faz E adayı** — `IUserDataScope` registry Mosaik.Core'a (cross-modül abstraction).

## Referanslar

- `Mosaik/Models/DataSource.cs` — entity
- `Mosaik/Services/UserDataFilterInjector.cs` — multi-tenant filter enforcement
- `Mosaik/Services/UserDataFilterValidator.cs`, `FilterDefinitionService.cs`, `FilterOptionsService.cs`
- `sqlcli.json` — connection profile (gitignored, dev tooling için)
- `memory/project_zirve_personel_discovery.md` — `vw_PersonelDepartman` keşfi
- `memory/project_bkm_sube_heterogen.md` — şube ID heterojenliği
- ADR-001 (SP + EF hibrit) — bu ADR onun "hangi DB" boyutunu netleştirir
- `.claude/rules/security-principles.md` §1 (SQL injection), §5 (secret management), §8 (multi-tenant filter)
- Plan 14, Plan 16.5, Plan 18, Plan 18B — bağlı planlar
