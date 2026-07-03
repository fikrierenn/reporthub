# Mosaik.Modules.GorevTanimlari

Pozisyon bazlı **görev tanımı** dokümanları + versiyon/yayın + **Zirve personel eşleme ekranı**.
Circular/SOP modül deseni (ASP.NET Core MVC Area + EF, IMosaikModule).

## Kanonik kayıt / karar / geçmiş — BAŞKA REPODA
Bu modülün içeriği, kararları ve tarihçesi **`D:\Dev\gorevtanimlari\bkm`** projesindedir (git değil):
- `DECISIONS.md` bölüm **G** (Mosaik/Zirve kararları) + **H** (bu modül + person-level mapping gerekçesi)
- `docs/journal/2026-07-02.md` (Oturum 3 — tam tarihçe) · `HANDOFF.md` · `CHANGELOG.md`
- `.claude/rules/work-protocol.md` — **HER İŞ: danış→yap→kontrol ettir→smoke** (zorunlu)
- md→DB göç: `bkm/scripts/migrate-to-mosaik.mjs`

**Bu repo (reporthub) tarafı:** `docs/journal/2026-07-02.md` → "Oturum 2 (Görev Tanımları modülü)" + `TODO.md` → "Görev Tanımları modülü" bloğu. reporthub session-end/başı ritüeli: `AGENTS.md §0` + `.Codex/rules/session-protocol.md` (bu modül de o disipline tabi: csharp/razor/security-conventions).

## Modül içeriği
- **Ekran:** `/GorevTanimlari/Mapping` — Zirve aktif personelini (vw_PersonelDepartman, IK datasource) listeler, her kişiye görev-tanımı seçilir. **Kişi-düzeyi** (`GorevPersonelMap.Personelno` STRING "4634-BKM") — aynı ünvan farklı tanıma gidebilir (Satın Alma Görevlisi → kişi Kırtasiye vs Oyuncak). Kişi eşlemesi yoksa rol-varsayılan (`GorevZirveMap` Departman+Unvan) önerilir.
- **Tablolar:** GorevDocuments · GorevVersions (SopVersions deseni, Status=taslak/yayın/süperse) · GorevZirveMap (rol) · GorevPersonelMap (kişi). Şema: `Database/01_GorevTanimlari.sql` (idempotent + AppModules kaydı).
- **IK datasource:** `DataSources.DataSourceKey='IK'` → Zirve BKM_GENEL (ZIRVE\ZRVSQL2008, **port 64507**; named-instance host'tan çözülmez).

## Çalıştır (dev)
```
cd D:\Dev\reporthub\Mosaik
$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run --urls http://localhost:5197
```
`Ai:WarmupEnabled=false` (appsettings.Development) → ONNX/LLM startup yüklemez. `/GorevTanimlari/Mapping` (admin login gerek).

## T-16 ✅ (2026-07-02 kapandı)
Birim test (MappingService.SaveAsync 4 dal, InMemory) · view inline-style→canonical class (.filter-bar/.inp/.check-inline/.activity/.dt/.alert.error/.status) · IndexVm → ViewModels/MappingIndexViewModel. Test 701/701 yeşil, code-review 0 CRIT/0 HIGH.

## Bekleyen
Doküman-editör ekranı (GorevVersions CRUD — tanım/KPI düzenle, yayınla=Rev N, SOP deseni). Ön-borç (platform): plaintext connstring (DataSources), inline SQL (sp_Ik*).
