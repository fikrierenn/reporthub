# ADR-027 — KVKK Process Backbone: Plan 38 EntityRelations ilk büyük tüketici

**Tarih:** 2026-07-01
**Statü:** Kabul edildi (Plan 40 tamamlandı — Faz 0-7)
**Karar verenler:** Fikri / Claude
**Bağlam:** [Plan 40 KVKK Veri Envanteri + Process Backbone](../../plans/40-kvkk-process-backbone.md), [Plan 38 EntityRelations](../../plans/38-entity-relations-decision-log.md)

---

## 1. Bağlam

BKM Kitap için KVKK 6698 envanter yükümlülüğü Excel'de tutuluyordu (361 süreç × 20 sütun). Plan 40, `KvkkProcess` central entity + `DataElement` granular envanter + `EntityRelations` (Plan 38) polymorphic linker ile bunu Mosaik'e taşıdı. `Mosaik.Modules.Kvkk` ayrı csproj (ADR-002/015).

## 2. Karar

**Modül izolasyon disiplini (ADR-002) KVKK'nın 7 fazında hiç ihlal edilmedi:**

1. **EntityRelations = çift-yazma, okuma = kendi junction.** Faz 4 kararı: modül EntityRelations'ı **okumaz** (Plan 38 omurga sadece cross-modül senkron kaydı), kendi strongly-typed junction'ını (`ProcessDataLink`, `SopProcessLink`) okur. Reverse lookup performansı + tip güvenliği için.
2. **Cross-modül bağ (SOP↔KVKK, Faz 3) = sıfır compile-time referans.** Junction sahipliği tek modülde (KVKK); karşı modül sadece cross-area HTTP fetch/POST ile etkileşir. `sopTitle` gibi client-supplied veri asla trust edilmez — raw SQL (`SqlQueryRaw<T>`) ile karşı taraf DB'den doğrulanır (compile-time tip referansı olmadan).
3. **Dashboard/Reports motoru reuse = UI pattern taklidi, altyapı DEĞİL (Faz 7).** Ana projenin SP-driven `DashboardRenderer`'ı EF-native KVKK verisiyle mixed-access yaratacağından (ADR-001 ihlali), KVKK kendi EF-native controller + view-local Chart.js kullandı.
4. **AI Integrity Checker (Faz 5) 7/8 pattern saf-C#.** Presidio (Docker yan-servis) ertelendi — footprint-ladder, 8 pattern regex+FuzzySharp+LINQ ile saf-C# yeterli bulundu.

## 3. Sonuç

- **Migration 01-07**, ~4000 satır C#, 646 test (proje toplamı) bozulmadan.
- **Plan 38 EntityRelations** ilk büyük canlı tüketicisi oldu — sözleşme (whitelist `EntityType`/`RelationType`, çift-yazma atomiklik) sertifiye edildi, genişletildi (`Sop`, `DerivedFrom` eklendi).
- **Faz 3/7 full-scan'lerde bulunan 3 gerçek bug** (EF `Contains(string,StringComparison)` çeviri hatası, cross-firma SOP içerik sızıntısı, sessiz "içerik yok" → "0 eşleşme" karışması) commit öncesi kapatıldı — faz döngüsü disiplini (danış→kod→scan→preview→commit) çalıştı.

## 4. Alternatifler (reddedilen)

- **KVKK verisini Reports/ReportCatalog SP altyapısına taşı** — mixed-access (aynı tablo EF+SP), ADR-001 çizgisini bulanıklaştırır. Reddedildi.
- **SOP↔KVKK bağını FK ile modelle** (herhangi bir tarafta) — ADR-002 modül-modül compile referansı yasağını ihlal eder. Reddedildi.
- **Presidio Docker (PII detection)** — footprint-ladder ihlali, 8 pattern için gereksiz altyapı. Ertelendi (gerekirse ayrı ADR).

## İlişkili

- [ADR-001](001-data-access.md) — data-access ayrımı (SP=rapor, EF=metadata)
- [ADR-002](002-modular-monolith.md) — modüler monolit, cross-modül referans yasağı
- [ADR-015](015-new-modules-separate-assembly.md) — yeni modül ayrı assembly
- [Plan 38](../../plans/38-entity-relations-decision-log.md) — EntityRelations omurga
- [Plan 40](../../plans/40-kvkk-process-backbone.md) — tam faz kaydı
