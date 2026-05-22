# Plan 45 — Post-Review Hardening (2026-05-22 dış inceleme)

**Tarih:** 2026-05-22
**Tier:** 2 (4 bağımsız Tier 2 fix bundle)
**Durum:** Onay bekliyor

## 1. Problem

Dış inceleme 2026-05-22 sonrası 4 doğrulanmış bulgu (HIGH x1, MEDIUM x3). Hiçbiri tek başına Tier 3 değil, ama bundle plan disiplini için tek dosya.

## 2. Bulgular (file:line doğrulandı)

### F-1 (HIGH) — DataSource delete audit ConnString sızıntısı
**Yer:** `Mosaik/Services/DataSourceManagementService.cs:95-106`
**Sorun:** `oldSnap = new { ds.DataSourceKey, ds.Title, ds.ConnString, ds.IsActive }` audit log `OldValuesJson` içine connection string gömüyor. Audit DB read access olan herkese leak.
**Fix:** `ds.ConnString` snapshot'tan çıkar. Audit'e sadece `DataSourceKey, Title, IsActive`. Edit path (line 80-82) zaten temiz, sadece delete (line 95) bozuk.

### F-2 (MEDIUM) — AI provider HTTP error body leak
**Yer:** `Mosaik/Services/Ai/MultiProviderVisionProvider.cs:140, 195`
**Sorun:** `$"Gemini vision HTTP {status}: {Truncate(text, 400)}"` ve `$"{providerName} vision HTTP {status}: {Truncate(text, 400)}"` — provider response body (quota detay, prompt parça, endpoint URL içerebilir) `AiSummaryResult.Error` field'ına gömülüyor. Wizard UI generic gösteriyor ama log/diagnostic taşıyıcılar leak'leyebilir.
**Fix:** Error message generic Türkçe ("Provider hatası — log'a bakın"), full body sadece `_logger.LogError` structured field'a. `AiSummaryResult.Error` kullanıcı-facing.

### F-3 (MEDIUM) — Forms cascade EF vs SQL uyuşmazlığı
**Yer:** `Mosaik.Modules.Forms/FormsModule.cs:111` (EF Cascade) vs `Mosaik.Modules.Forms/Database/70_FormsSchema.sql:162-164` (SQL FK cascade yok)
**Sorun:** EF code-first Cascade davranışı bekler ama production DB FK SQL'inde `ON DELETE CASCADE` yok. EF migration uygulanmayan ortamda `FormDefinition` silme → FK violation 547.
**Fix:** Yeni migration `72_FormsTokenCascade.sql` — `ALTER TABLE PublicFormTokens DROP CONSTRAINT FK_...; ADD CONSTRAINT ... ON DELETE CASCADE`. Idempotent.

### F-4 (MEDIUM) — Forms DB invariant zayıflığı
**Yer:** `Mosaik.Modules.Forms/Database/70_FormsSchema.sql:32, 58, 83, 108, 155-167`
**Sorun:** Enum byte field'larında (`Status`, `FieldType`, `SubmissionStatus`) CHECK constraint yok; `PublicFormTokens.UsedCount <= MaxUses` invariant yok; `FormSubmissionFieldValues` "exactly one value column" invariant yok. Runtime validation Faz 1'de gelecek olsa bile DB seviyesinde guard değerli.
**Fix:** Yeni migration `73_FormsDbInvariants.sql` — CHECK constraint ekle (idempotent: `IF NOT EXISTS (sys.check_constraints WHERE name=...)`). Enum range, UsedCount<=MaxUses, value column XOR.

### F-5..F-7 (LOW) — Backlog
- F-5 AI budget race (concurrent overshoot) → `AiSummaryProvider.cs:47,103` Interlocked atomik check-and-add gerek. Multi-instance zaten known. **Plan 33 Faz 2 D-02 follow-up'a not düş.**
- F-6 inline onclick (CSP debt) → mevcut envanter, sweep zaten Plan 25 sonrası. **TODO.md frontend debt'e ekle.**
- F-7 büyük dosyalar (EditReportV2 1006, CreateReportV2 913) → Reports modülü yeniden ele alındığında. **TODO.md M-01 follow-up'a ekle.**

## 3. Scope

### Dahil
- F-1 fix (Edit + 1 test ekle: ConnString audit'te yok)
- F-2 fix (2 satır + structured log)
- F-3 yeni migration 72
- F-4 yeni migration 73

### Hariç
- F-5/F-6/F-7 backlog'a not, bu plan kapsam dışı
- Audit log tablosu eski leak verisi temizleme (ayrı iş, kullanıcı kararı)

### Etkilenen dosyalar
- `Mosaik/Services/DataSourceManagementService.cs` (1 satır)
- `Mosaik/Services/Ai/MultiProviderVisionProvider.cs` (2 satır + _logger structured)
- `Mosaik.Modules.Forms/Database/72_FormsTokenCascade.sql` (yeni)
- `Mosaik.Modules.Forms/Database/73_FormsDbInvariants.sql` (yeni)
- `Mosaik.Tests/DataSourceManagementServiceTests.cs` (yeni veya append) — audit content assertion
- `TODO.md` (F-5/6/7 backlog ekle)

## 4. Done criteria

- [ ] F-1: `oldSnap` `ConnString` yok; audit JSON'da `ConnString` key yok (test ile assert)
- [ ] F-2: `AiSummaryResult.Error` generic Türkçe; structured `_logger.LogError("Provider X HTTP {Status} Body={Body}", ...)` çağrısı her iki path'te
- [ ] F-3: Migration 72 idempotent; DB'de FK `FK_PublicFormTokens_FormDefinitions` ON DELETE CASCADE ile var
- [ ] F-4: Migration 73 idempotent; CHECK constraint'ler DB'de var
- [ ] dotnet test 411+ yeşil (yeni audit assertion testi ile +1)
- [ ] TODO.md F-5/6/7 backlog satırları eklendi

## 5. Riskler

| Risk | Etki | Mitigation |
|---|---|---|
| F-3 migration prod DB'de mevcut child kayıtla çakışır | düşük | Mevcut DB'de PublicFormTokens boş (Plan 41 Faz 0, kullanıma açılmadı) |
| F-4 mevcut veride invariant ihlali → ALTER ADD CHECK fail | düşük | Forms tabloları boş (canlı kullanım 0) |
| F-2 yanlış generic mesaj wizard UX bozar | düşük | UI zaten generic gösteriyor (Plan A-04+); değişiklik error field'da |

## 6. Adımlar

1. **S-01** F-1 fix (`DataSourceManagementService.cs:95` ConnString çıkar)
2. **S-02** F-1 test (`DataSourceManagementServiceTests` audit assertion)
3. **S-03** F-2 fix (`MultiProviderVisionProvider.cs:140,195` generic msg + structured log)
4. **S-04** Migration 72 (Forms token cascade)
5. **S-05** Migration 73 (Forms DB invariants)
6. **S-06** dotnet test → 412/412 yeşil
7. **S-07** TODO.md F-5/6/7 backlog satırları
8. **S-08** Commit bundle: `fix(security): F-1..F-4 post-review hardening (plan: 45)`

## 7. Onay

- [ ] Plan kullanıcıya gösterildi: 2026-05-22
- [ ] Onay alındı: ___
