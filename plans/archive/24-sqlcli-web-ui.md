# Plan 24 — sqlcli Web UI (SSMS-Benzeri Tarayıcı Arayüzü)

**Tarih:** 2026-05-10
**Yazan:** Claude
**Durum:** `Taslak`

---

## 1. Problem

Kullanıcı kararı: "sqlcli için bir arayüzde yapabilir misin bir ara ms benzeri".

Mevcut `D:\Dev\sqlcli` CLI tool: query, script, browse-schema, describe-table, list-databases destekliyor. Konsol-only kullanım — sonuç tablosu ANSI escape'lerle kirli, büyük tablo'lar terminal'de okunmaz, profil bazlı bağlantı switch zor (env değişkeni / json), query history yok, çoklu tab yok, sonuç export (csv/excel/json) yok.

Kullanıcı "ms benzeri" dedi — SSMS (SQL Server Management Studio) referansı: Object Explorer (sol panel), Query Editor (orta), Result Grid (alt).

## 2. Scope

### Kapsam dahili
- Yeni proje: `D:\Dev\sqlcli-web` veya Mosaik altında `Mosaik/Areas/SqlConsole/` (karar: Mosaik içinde admin-only area).
- Sol panel: Profile dropdown (sqlcli.json profilleri) + database tree (DB → Tables/Views/Procs) — `mcp__sqlserver__sql_browse_schema` pattern.
- Orta panel: Monaco Editor (T-SQL syntax highlight + IntelliSense). Run/Cancel/Save buton.
- Alt panel: Result grid (sortable, copy-cell, export csv/json). Multi-tab (her query ayrı tab).
- Backend: `Mosaik/Controllers/SqlConsoleController.cs` admin-only — `SELECT/SHOW/DESCRIBE` whitelist + parameterized + connection string profilden alır. `EXEC sp_*` admin tarafından onaylanmış SP listesinden seçilebilir.
- Query history: `dbo.SqlConsoleHistory` tablosu (UserId, Profile, QueryText, Duration, RowCount, ExecutedAt).
- Object Explorer: tablo sağ-tık → "Top 100 satır", "Schema göster" — dinamik kuyruk üretir (parameterized).
- DELETE/UPDATE/INSERT/DDL: **kapatılabilir** (admin setting `SqlConsole.AllowMutations`). Default `false`. True olunca uyarı modal + confirm.

### Kapsam dışı
- Tam IntelliSense (kolon tamamlama) — Monaco basic T-SQL grammar yeterli.
- Query plan / execution plan görselleştirme — SSMS'in flagship özelliği, YAGNI.
- Bağlantı şifre yönetimi — sqlcli.json mevcut (Windows Auth + dev SA).
- Multi-user collaboration / paylaşılan query.
- Saved queries / snippet library.
- BkmArgus benzeri row-level audit log.

### Etkilenen dosyalar
- `Mosaik/Areas/SqlConsole/Controllers/SqlConsoleController.cs`
- `Mosaik/Areas/SqlConsole/Views/Index.cshtml` (Monaco + tree + grid)
- `Mosaik/Areas/SqlConsole/Services/SqlConsoleService.cs` — execution + history
- `Mosaik/Database/55_CreateSqlConsoleHistory.sql`
- `Mosaik/wwwroot/assets/js/sql-console/*.js` — Monaco + tree + grid
- `Mosaik/wwwroot/lib/monaco-editor/` — Monaco bundle (CDN ya da local)
- `Mosaik/Models/AppModule.cs` seed: `sqlconsole` ModuleKey
- `Mosaik/Program.cs` — Area mapping

**Tahmini boyut:** 12 dosya / ~1000 satır (Monaco entegrasyon ağır).

## 3. Alternatifler

### A: Bağımsız proje (`D:\Dev\sqlcli-web`)
**Açıklama:** Ayrı .NET app, sqlcli'yi referans alır.
**Reddetme sebebi:** Auth ekosistemi sıfırdan, deploy iki uygulama, brand/menu duplicate. Mosaik'in admin paneli zaten var.

### B: Hazır open-source tool (Adminer, phpPgAdmin SQL Server portu)
**Açıklama:** Adminer SQL Server destekli, tek dosya PHP.
**Reddetme sebebi:** PHP runtime + IIS module, BKM kurumsal AV potansiyel sorun. Auth integration yok. Türkçe UI yok.

### C: Mosaik altında admin-only Area + Monaco (SEÇİLEN)
**Açıklama:** Mosaik içinde Area, mevcut auth/role/audit/brand reuse, Monaco CDN.
**Sebep:** Tek deploy, tek auth, mevcut sidebar'a entegre, audit log + role gate hazır altyapı.

### D: Daha basit textarea + rendered table (Monaco yerine)
**Açıklama:** Monaco CDN kalkar, plain `<textarea>` + result grid.
**Reddetme sebebi:** Kullanıcı "SSMS benzeri" dedi — syntax highlight olmazsa "ms benzeri" değil, sıradan form olur.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| SQL injection / mutation kötüye kullanım | yüksek | yüksek | Default `AllowMutations=false`. Whitelist regex `^\s*(SELECT\|EXEC\|SHOW\|DESCRIBE)\b`. Audit log her query. Role: `[Authorize(Roles="admin,sql")]`. |
| Connection string sızması | yüksek | düşük | Profile sadece backend'de saklanır, JS'e gönderilmez. UI'da sadece profile name görünür. |
| Büyük result set tarayıcıyı dondurur | orta | yüksek | Backend `TOP 1000` enforce + warning. Export tam sonuç stream'le csv. |
| Monaco bundle 2MB+ | düşük | yüksek | CDN load (jsdelivr), Mosaik static asset boyutu büyümez. Lazy load Area girince. |
| sqlcli.json profilleri Mosaik DB'sine erişim verir | yüksek | yüksek | Default profile sadece `master + DerinSIS + BKMDATA + EncoreMerkez + BKM` (Mosaik DB allowlist DIŞINDA, mevcut MCP allowlist ile aynı). Mosaik DB'ye SQL Console üzerinden erişim YASAK (ayrı dotnet ef console kullanılır). |
| Çoklu kullanıcı concurrent connection limit | düşük | orta | Connection pool default. Heavy query timeout 60sn. |

## 5. Done Criteria

- [ ] `/SqlConsole` admin-only erişilir, Monaco editor yüklenir.
- [ ] Profile dropdown sqlcli.json'dan beslenir.
- [ ] DB tree expand edilince tablolar listelenir, sağ-tık → Top 100.
- [ ] SELECT query çalışır, sonuç grid'de gösterilir.
- [ ] Mutation default kapalı; UPDATE denenince "AllowMutations=false, izin yok" hatası.
- [ ] Query history tablosuna kayıt yazar.
- [ ] CSV/JSON export çalışır.
- [ ] Audit log her execute için satır yazar (`sqlconsole_query_run`).
- [ ] Mosaik DB'ye query atılamaz (allowlist gate).

## 6. Rollback Planı

- Git revert.
- Migration 55 down: `DROP TABLE SqlConsoleHistory`.
- AppModule seed `sqlconsole` row sil.
- Area routing kaldır.

## 7. Adımlar (Faz A → D)

### Faz A — Backend altyapı (~6h)
1. [ ] `SqlConsoleController` + `SqlConsoleService` (whitelist + profile + audit)
2. [ ] Migration 55 `SqlConsoleHistory` tablosu
3. [ ] Profile loader (sqlcli.json paralel kullanım)
4. [ ] Allowlist gate (Mosaik DB'ye erişim ban)

### Faz B — Frontend Monaco + Tree (~8h)
5. [ ] Monaco CDN + T-SQL grammar yükleme
6. [ ] DB tree (Alpine.js + recursive expand) + browse-schema endpoint
7. [ ] Result grid (sortable, copy-cell)
8. [ ] Multi-tab query editor

### Faz C — Polish + güvenlik (~4h)
9. [ ] Query history UI panel
10. [ ] CSV/JSON export
11. [ ] AllowMutations admin setting + warning modal
12. [ ] Sidebar AppModule seed (Plan 23 ile uyum)

### Faz D — Test + dokümantasyon (~2h)
13. [ ] xUnit test: whitelist + allowlist + audit
14. [ ] CLAUDE.md/.claude/rules ekleme

**Toplam tahmin: ~20 saat (büyük plan, faz-faz geliştirilebilir).**

## 8. İlişkili

- `D:\Dev\sqlcli` mevcut CLI projesi (referans, sqlcli.json paylaşılır)
- Plan 23 (Sidebar) — `sqlconsole` ModuleKey + GroupKey "Sistem"
- ADR planı: `docs/ADR/012-sql-console-area.md`
- BkmArgus query audit pattern referans (Plan 16 keşfinden)

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Onay alındı: ___

---

## NOT: Faz A önce — minimum çalışır iskelet

Plan büyük (20h). Önerilen ilk PR: sadece **Faz A + Faz B'nin minimum'u** (Monaco + tek profile + SELECT-only + result grid). Faz C+D ileri sprint.
