# Plan 55 — BKM Org Yapısı Import (org.json → OrgPositions reuse)

**Tier:** 3 (schema + cross-modül + kullanıcı-görünür /OrgChart + veri backfill)
**Durum:** ⏳ TASLAK 2026-07-03 — onay bekliyor
**Danışman:** `mosaik-portal-danismani` — A (OrgPositions reuse) FİZİBİL, **confidence 82**
**Bağımlılık:** GorevDocuments(48)+GorevVersions(48) canlı; `GorevDocument.OrgPositionId` FK zaten şema-hazır (`01_GorevTanimlari.sql:15`)

---

## Problem

BKM'nin gerçek org yapısı (`D:\Dev\gorevtanimlari\bkm\data\org.json` — 53 düğüm, 9 müdürlük, kişi-merkezli ağaç + `def` görev tanımı) Mosaik'e **kopuk** girmiş:
- **`OrgPositions` (62 satır) = ESKİ** generic ünvan seed (migration 40, Zirve distinct) — org.json değil.
- **`GorevDocuments` (48) = yeni içerik ama DÜZ** — parent yok, `OrgPositionId` hepsi NULL.
- Canlı `/OrgChart` eski 62 ünvanı gösteriyor; holder kişiler + görev tanımı bağı yok.

Hedef: `/OrgChart`'ta BKM'nin gerçek 53-düğüm yapısı + holder kişiler + tıklayınca görev tanımı.

## Scope

**Karar (kullanıcı 2026-07-03):** _"Zirve ile eşleştireceğim için ayrı orgchart gereksiz; bizimki ana omurga olmalı."_ → **org.json (bizim 48-53 tasarım) = ana omurga = `OrgPositions`.** Zirve personeli bu omurgaya EŞLENİR (GorevPersonelMap kişi-düzeyi + Zirve incumbent overlay), omurga Zirve'den türetilmez. Ayrı org-chart ekranı YOK — tek `/OrgChart`.

> **⚠️ DECISIONS G supersede:** bkm `DECISIONS.md` G maddesi "OrgPositions = Zirve aynası (Direktör-modeli)" diyordu. Bu karar onu **geçersiz kılar** — omurga bizim tasarım, Zirve maps-in. bkm DECISIONS.md'ye "Bölüm I" olarak işlendi (2026-07-03). Eski 62 Zirve-türevi satır → IsActive=0.

**Mevcut `OrgPositions` tablosunu yeniden kullan** (footprint-ladder basamak-1: tabloyu genişlet, yeni tablo yok). `/OrgChart` render + Zirve-incumbent (artık ZirveMatchKey ile) + CRUD altyapısı korunur; içerik omurga = bizim org.json.

**Dokunulacak:**
- `Mosaik/Database/` — OrgPositions ALTER (3 kolon) + org.json rebuild seed
- `Mosaik.Core/Domain/OrgPosition.cs` + `MosaikContext` — yeni kolonlar
- `Mosaik/Services/OrgChartService.cs` — Zirve match `Code` → `ZirveMatchKey`
- `Mosaik/Views/OrgChart/Index.cshtml` — holder + görev tanımı bağ (render)
- `D:\Dev\gorevtanimlari\bkm\scripts\migrate-to-mosaik.mjs` (içerik tarafı) — SQL generator

**Kapsam dışı:** Yeni org-tree tablosu (B reddedildi); GorevVersions editör (Plan ayrı); eski 62 satır hard-delete.

## Doğrulanmış veri (canlı DB + org.json)

- org.json: **53 düğüm** (`walk` ile), hepsinde `def={paragraphs,kpi}` + `name`(kişi) + `position`(ünvan) + `id`(n0..) + `directToManager`(7).
- **5 çift-ünvan:** 2× GMY / Kampanya Uzman Yrd / Sosyal Medya Uzmanı / İK Uzmanı / Ön Muhasebe Uzmanı.
- GorevDocuments: **48** = 53 − 5 merge. GMY kişi-suffix'li 2 doc (id 4,5); diğer 4 çift-ünvan tek doc'a merge.
- OrgPositions kolon: Id, Code(UNIQUE), Title, ParentPositionId(self-ref FK NO ACTION), DisplayOrder, IsActive, Description. **Holder kolonu yok.**
- Zirve incumbent: `OrgChartService.GetChartWithIncumbentsAsync` (`OrgChartService.cs:327-417`) `Code==Zirve.Unvan` string-match.

## Alternatifler (5 lens)

- 🔴 **Contrarian (fatal flaw):** Code'a suffix ekleyince Zirve title-match sessizce kırılır → tüm incumbent "unmatched". Mitigasyon: `ZirveMatchKey` kolonu + service match değişimi **aynı migration'da**.
- 🔵 **First-principles:** Gerçek problem "org.json ağacı DB'de değil" — OrgPositions zaten self-ref ağaç + render + FK hazır; yeni tablo gereksiz.
- 🟢 **Expansionist:** OrgPositions reuse, holder + görev tanımı + KPI bağını tek org şemasında toplar (VISION org backbone).
- ⚪ **Outsider:** "Neden iki org tablosu?" — B (modül-içi tree) tam bunu yaratırdı; A tek canonical tablo.
- 🟡 **Executor (ilk adım):** Faz 1 — OrgPositions'a 3 kolon ALTER + ZirveMatchKey=Title backfill (hiçbir şey kırılmadan).

**B (modül-içi yeni org-tree) REDDEDİLDİ:** cross-modül FK zaten var (izolasyon ihlali değil — OrgPositions Mosaik.Core paylaşılan çekirdek); `/OrgChart` UI'ı Core'dan koparmak israf.

## ZORUNLU 3 şema değişikliği (danışman)

1. **Code UNIQUE çakışması** → org.json `id` suffix (`Genel Müdür Yardımcısı-n3`). Title temiz kalır (UI'da suffix gösterilmez).
2. **`HolderName` (nvarchar 150) + `HolderPersonelno` (nvarchar 50) NULL** → org.json `name` (+ ileride Zirve kişi bağı).
3. **`ZirveMatchKey` (nvarchar 150) NULL** → temiz ünvan; `OrgChartService` match'i buraya taşınır (yoksa incumbent kırılır — YÜKSEK risk).

## Fazlar

### Faz 1 — OrgPositions şema genişletme (reporthub)
- `Mosaik/Database/81_AlterOrgPositions_HolderMatchKey.sql` — idempotent ADD `HolderName`, `HolderPersonelno`, `ZirveMatchKey`. Mevcut 62 satır: `ZirveMatchKey = Title` backfill (match korunur).
- `OrgPosition.cs` + `MosaikContext` OnModelCreating — 3 property.
- **Nav property EKLEME** (`GorevDocument` referansı) — skalar `int? OrgPositionId` kalsın (izolasyon).

### Faz 2 — OrgChartService match-key geçişi (reporthub) + test
- `OrgChartService.cs:348-350,383` — incumbent match `Code` → `ZirveMatchKey` (null ise `Title` fallback).
- Test: mevcut `OrgChartServiceTests` yeşil + ZirveMatchKey match testi. **Bu faz Faz 4'ten ÖNCE** (suffix gelmeden match-key devrede).

### Faz 3 — org.json → SQL generator (içerik = bkm)
- `bkm/scripts/migrate-to-mosaik.mjs` genişlet — emit:
  - (a) OrgPositions rebuild: 53 düğüm, `Code`=title+`-{id}` (yalnız çift-ünvanlarda suffix; tekil temiz), Title, ParentPositionId(ağaçtan), HolderName=name, ZirveMatchKey=position, DisplayOrder=sıra.
  - (b) `GorevDocuments.OrgPositionId` UPDATE — 48 doc → OrgPosition (id eşleme; 4 merge-doc birincil düğüme, GMY 1:1).
  - (c) eski 62 → `IsActive=0` (soft-deactivate, **hard-delete yok**).
- Çıktı: `bkm/mosaik-migration/03-orgpositions.sql` (GO batch'li).

### Faz 4 — migration uygula (reporthub)
- **Backup önce** (`feedback_yedek_almadan_silme_yok`): OrgPositions + GorevDocuments SELECT INTO yedek.
- sqlcli ELLE + GO batch ayrı. Doğrula: 53 aktif pozisyon, 48 doc `OrgPositionId NOT NULL`, incumbent match sayısı ≥ eski (mass-unmatched yok).

### Faz 5 — render: holder + görev tanımı bağ (reporthub)
- `Views/OrgChart/Index.cshtml` — node'da HolderName göster; tıkla → görev tanımı (GorevVersion içeriği). 4 çift-ünvan sibling: görev tanımı ZirveMatchKey/Title match ile çözülür.
- Preview E2E (admin/123456): /OrgChart 53-düğüm + holder + tıkla-tanım + incumbent panel sağlam.

### Faz 6 — kapanış
- `dotnet test` yeşil. ADR notu (org canonical = org.json via OrgPositions). bkm CHANGELOG + journal + TODO ✅.

## Riskler

| Risk | Sev | Mitigasyon |
|---|---|---|
| Zirve match kırılır (Code suffix) | YÜKSEK | `ZirveMatchKey` + service değişimi Faz 2'de, suffix Faz 4'te — sıra kritik |
| PositionCode==Code kırılgan eşleme | ORTA | org.json `id` tabanlı eşleme (string title değil) |
| 4 çift-ünvan doc paylaşımı | ORTA | doc birincil düğüme; sibling render'da title match |
| Eski veri kaybı | DÜŞÜK | soft-deactivate + backup, hard-delete yok |
| Elle CRUD kayıtları (62 üstü admin edit) | DÜŞÜK | IsActive=0 korur, silmez |

## Rollback
Migration geri-alınabilir: eski 62 `IsActive=1` + yeni 53 `IsActive=0` + `OrgPositionId=NULL` + 3 kolon DROP + service match `Code`'a geri. Backup tablodan restore.

## Done Criteria
- [ ] /OrgChart 53-düğüm BKM ağacı + holder kişiler render
- [ ] GorevDocuments 48 `OrgPositionId NOT NULL` (görev tanımı ↔ pozisyon bağlı)
- [ ] Zirve incumbent match sağlam (mass-unmatched yok)
- [ ] Node tıkla → görev tanımı içeriği
- [ ] `dotnet test` yeşil, preview E2E doğrulandı
