# Plan 33 — Modül Tamamlama Roadmap (vNext öncesi temizlik)

**Tarih:** 2026-05-14
**Yazan:** Fikri / Claude
**Durum:** ✅ Onaylandı (kullanıcı 2026-05-14 "bunları detaylı işle")

**Tier:** 3 (meta-plan — 10+ modül etkilendi, schema/security/UX değişiklikleri, 5+ klasör)
**Tahmini süre:** ~85-120 saat = 3-4 hafta (paralel iş ile 4-5 hafta)
**Bağlam:** 5 paralel keşif agent (2026-05-14 oturumu) sonrası mevcut modüllerde **kritik bugfix'ler, plan stale ve eksik fazlar** tespit edildi. VISION'da vNext kalbi (Plan 34 SOP + Plan 35 Comment + Plan 36 Workflow Designer) başlamadan önce **mevcut zemini temizlemek** stratejik karar. Kullanıcı 2026-05-14: "diğer modüller için detaylı planlarını yap eksiklikleri tara onları bitirip sop a başlayacağız."

---

## 1. Problem

5 modül grubu derin keşif edildi:
- **Documents (Plan 27):** Faz A %80, Faz B %70, Faz C+D+E %0 — kritik: wwwroot file serving güvenlik açığı
- **Contracts + Obligations + AI Wizard (Plan 25+25.1):** %95 tamam ama 2 kritik bug + Plan 25.1 Faz 5+6 açık + Wizard controller endpoint yok
- **Calendar + Compliance + Notifications:** Plan 22 %0, cross-modül entegrasyon yok, DailyReminderJob yok
- **AI altyapı:** 7 provider çalışıyor ama vision tekil, audit log eksik, token bütçe yok
- **Tamim + OrgChart:** Done iddiası gerçek ama plan dosyaları stale (kutucuklar `[ ]`)

Ayrıca 7 plan dosyası gerçeklikle uyuşmuyor (stale veya superseded).

**Sistemik risk:** SOP modülüne (Plan 34) başlamadan bu eksiklikleri kapatmazsak vNext kalbi **kırık zemin üzerine** kurulur — Notification cross-modül yokken SOP read deadline reminder çalışmaz, Calendar unified değilken SOP'un takvim entegrasyonu yarım olur, AI altyapı audit log'u yokken SOP AI Danışman istatistiksiz kalır.

## 2. Scope

### Kapsam dahili — 4 faz

**Faz 1 — Plan stale temizlik + Kritik bugfix'ler** (~7-10 saat, 1-2 gün)
- 7 stale plan archive veya durum güncelle
- 4 kritik bugfix (data integrity + güvenlik)

**Faz 2 — Modül tamamlama** (~37-50 saat, 1-1.5 hafta)
- C1-C8: DailyReminderJob, Plan 22 Holidays, Notification cross-modül, AI Wizard Faz 1, ContractsController split, Obligations.Edit, Plan 27 Faz A, Plan 25.1 Faz 5+6

**Faz 3 — Mimari kararlar** (~4-6 saat, ayrı oturum)
- ADR-016 Calendar unified event source
- ADR-017 Compliance scope sınırı
- VISION güncelleme (AI Generation karar notu)

**Faz 4 — Büyük borç (ertelenebilir, opsiyonel)** (~38-52 saat, 2-3 hafta)
- D1: Documents Plan 27 Faz C (versioning, FTS, metadata, permission)
- D2: AI altyapı iyileştirme (audit log, token bütçe, rate limiting, FallbackLlmService cleanup)
- D3: Vision çoklaştırma (Gemini + OpenAI fallback)
- D4: Test coverage (Documents/Wizard/multi-firma integration)
- **Plan 16.7 Tag system** Faz A bağımsız (8-10sa) burada opsiyonel

### Kapsam dışı

- **Plan 34 (SOP)** — Faz 4 sonrası başlar (vNext kalbi, VISION sıralaması)
- **Plan 35 (Comment/Mention) + Plan 36 (Workflow Designer)** — SOP ardından
- **Documents Plan 27 Faz D+E** (compliance + RAG) — uzun vadeli, ayrı sprint
- **VISION yapılmayacaklar listesi** (KPI/OKR, Mesajlaşma, ayrı Duyuru, own Form Builder) — değişmez

### Etkilenen dosyalar (özet)

- 7 plan archive/güncelleme
- 4 bugfix (AiController, Review.cshtml, DocumentsController, WizardExtractionService)
- ~12 yeni dosya (Holiday entity + service + job + Wizard controller + Notification caller'lar)
- ~15 mevcut dosya düzenleme (ContractsController split, Plan 25.1 Faz 5, audit log eklemeleri)
- ~6 migration (Holiday, ImportantDate, calendar view, Document Faz C entity'leri ileride)
- ~2 ADR (016, 017)

**Tahmini boyut:** ~50 dosya / ~3000-4000 satır net değişiklik.

---

## 3. Faz 1 — Plan stale + Kritik bugfix (~7-10 saat)

### 3.1 Plan stale temizlik (~1-2 saat)

| Plan | Aksiyon | Komut |
|---|---|---|
| 16.6 Module extension | Archive (Circular implementation done) | `git mv plans/16.6-...md plans/archive/` + Durum: "Implement edildi (Circular pattern)" |
| 17 Tamim | Done criteria güncelle, archive | Faz H eksik kapsam → Plan 17.1 mini plan, sonra archive |
| 20 OrgChart | Faz C kutucuk onay, archive | `[ ]` → `[x]`, archive'a taşı |
| 21 Lookup | Durum güncelle | LookupService canlı, "Faz A+B implement edildi" |
| 23 Sidebar | Archive | Implementation tamam |
| 25 Sözleşme | Durum güncelle | "Faz A+B+C kısmen tamam, 25.1'e bağlı" |
| 25.1 Hardening | Durum güncelle | "Faz 1-4 tamam, Faz 5+6 açık" |
| 26 OCR fallback | Archive (Plan 27 Faz A absorb) | SUPERSEDED notu + `git mv archive/` |
| 27 Documents AI | Durum güncelle | "Faz A %80, Faz B %70, Faz C+D+E backlog" |

### 3.2 Kritik bugfix'ler (~6-8 saat)

**BUGFIX-1: `AiController.cs:446` data integrity bug** (30dk)
- `s.CreatedObligationId = -1` placeholder kalıcı yazılıyor
- Fix: `_db.SaveChangesAsync()` sonrası foreach ile gerçek ID güncelle
- Test: integration test obligation cross-link doğrulama

**BUGFIX-2: `Review.cshtml:126` iframe security** (15dk)
- `src="/@Model.ContractFile.FilePath"` → `src="/Contracts/Download/@Model.ContractFile.Id"`
- Download endpoint zaten App_Data + auth + path guard içeriyor

**BUGFIX-3: `DocumentsController.Upload` güvenlik fix** (2-3 saat)
- Path: `WebRootPath/uploads/` → `ContentRootPath/App_Data/documents/`
- `Index.cshtml:181` `href="/@f.FilePath"` → `href="/Documents/Download/@f.Id"`
- Yeni `GET Download/{id}` endpoint (auth + firma check + path guard, ContractsController.Download pattern reuse)
- Mevcut dosyaları taşıma migration (opsiyonel — yeni upload'lar App_Data'ya, eskiler kalsın)

**BUGFIX-4: `WizardExtractionService` taranmış PDF crash** (2-3 saat)
- `cs:113` exception fırlatma yerine TesseractOcrExtractor chain
- AiExtractionWorker pattern reuse (PageImportanceScorer + per-page confidence + vision rescue)
- Wizard timeout artır (120s vision için)

---

## 4. Faz 2 — Modül tamamlama (~37-50 saat)

### C1 — DailyReminderJob (3-4 saat)

Tek Hangfire RecurringJob, 3 modülü açar:
- Calendar event reminders
- ContractObligation due-date → Notification + Email
- Compliance periyodik obligation üretimi
- Plan 31 SMTP caller (Plan 32 önkoşulu)

**Implementation:**
- `Mosaik/Services/DailyReminderJob.cs` (~150 satır)
- `Program.cs` `RecurringJob.AddOrUpdate<DailyReminderJob>(...)` 08:00 cron
- `IEmailService` + `INotificationService` inject

### C2 — Plan 22 Holidays (~10-14 saat)

Calendar'ı unified event source yapar.

**Entity:** `Holiday`, `HolidayOccurrence`, `ImportantDate` (3 yeni)
**Migration:** 58_Holidays.sql, 59_ImportantDates.sql, 60_CalendarUnifiedView.sql (vw_CalendarUnified)
**Servis:** `HolidayService`, `ImportantDateService`
**EventSource enum genişlet:** `Manual`, `AiSuggested`, `Obligation`, `Holiday`, `ImportantDate`
**View:** Kaynak filtre chip (toggle)
**ADR-016** önce yazılır (Calendar unified karar).

### C3 — Notification cross-modül caller'lar (4-6 saat)

DailyReminderJob (C1) altyapısı üzerine:
- `ObligationsController` due-date geçince push
- Tamim deadline (Plan 17 Faz H tam kapsam — hatırlatma cron 09:00)
- Plan 32 Scheduled Reports email caller (6 açık soru kapatılınca)

### C4 — AI Wizard Faz 1 tamamla (8-10 saat)

- `AiController.WizardStart` endpoint (upload → start)
- `AiController.WizardStatus` polling
- `Views/Ai/WizardForm.cshtml` UI (suggestion review + form alanı doldur)
- BUGFIX-4 Tesseract chain entegrasyonu

### C5 — ContractsController partial split (1 saat)

- `ContractsController.Files.cs` — UploadFile, Download, magic byte, sanitize
- `ContractsController.Ai.cs` — StartAiExtraction, ReviewExtraction
- Ana: Index, Details, Create, Edit, Accessible firma
- 577 satır → 3 dosya × ~200 satır

### C6 — ObligationsController.Edit (2 saat)

- `GET Edit/{id}` + `POST Edit/{id}` + `Views/Obligations/Edit.cshtml`
- Mass-assignment koruması (ObligationEditViewModel)

### C7 — Plan 27 Faz A eksikleri (4-6 saat)

- `ContractExtractionValidator.cs` — JSON schema + 1 retry
- Stage 3 doğrulama çağrısı (null alan için targeted retry)
- 10 field-specific prompt registry (CUAD pattern, Plan 27 Faz A-06)

### C8 — Plan 25.1 Faz 5+6 (5-7 saat)

- **Faz 5:** `WizardExtractionService.cs:65` `Task.Run` → `AiPipelineQueue` enqueue
- **Faz 6:**
  - Inline style admin view'lar: `CreateUser.cshtml` (39), `EditUser.cshtml` (40), `_AppLayout.cshtml` (4)
  - `.claude/hooks/inline-style-guard.sh` enable
  - Smoke test + compliance scan

---

## 5. Faz 3 — Mimari kararlar (~4-6 saat)

### ADR-016 — Calendar Unified Event Source

**Karar:** Calendar her modülden gelen event'leri **SQL view (`vw_CalendarUnified`)** ile birleştirir. Her modül kendi event tablosunu yazar (Holiday/ImportantDate/ContractEvent/CircularDeadline), view UNION ALL ile sunar, CalendarController view'dan okur. Yeni kaynak eklenince controller dokunulmaz.

**Alternatifler:**
- A) Her modül `ContractEvent` tablosuna yazar → tablo kirlilik, FK karmaşası
- B) Calendar tek event entity, polymorphic — şişer
- C) View tabanlı union (seçilen)

### ADR-017 — Compliance Scope Sınırı

**Karar:** `ComplianceTemplate` = "Obligation şablon deposu + import wizard". Ayrı KVKK modülü değil. KVKK/ISO 27001:
- Sözleşme bazlıysa → `ContractObligation.Category=Legal`
- Şirket geneliyse → `ComplianceTemplate` import

**Alternatifler:**
- A) Ayrı Compliance modülü (KVKK checklist, ISO 27001 dokümanları) — overkill
- B) Compliance template + import (seçilen)
- C) Tüm uyum sözleşme altında — şirket geneli kaybedilir

### VISION güncelleme — AI Generation karar

VISION §3 "AI generation yarım" → karar notu eklenir: **Scope tut, bağımsız servis YAGNI. SOP/Tamim modülünde "taslak üret" butonu ileride.**

---

## 6. Faz 4 — Büyük borç (opsiyonel, ertelenebilir)

| # | İş | Effort | Aciliyet |
|---|---|---|---|
| D1 | Documents Plan 27 Faz C — versioning + FTS + metadata + permission + audit | 20-28sa | Orta (SOP'tan önce gerek değil) |
| D2 | AI altyapı iyileştirme — audit log + token bütçe + rate limiting + cleanup | 8-10sa | Orta (SOP AI Danışman'da iyi olur) |
| D3 | Vision çoklaştırma — Gemini + OpenAI fallback | 4-6sa | Düşük |
| D4 | Test coverage — Documents/Wizard/multi-firma | 6-8sa | Orta |
| D5 | Plan 16.7 Tag Sistemi Faz A | 8-10sa | Düşük (vNext aşamasında değerli) |

**Önerim:** D2 (AI iyileştirme) Faz 2'den hemen sonra yapılsın — SOP AI Danışman'a temiz zemin verir. D1+D3+D4+D5 SOP paraleline kaydırılabilir.

---

## 7. Done Criteria

- [ ] 7 plan stale temizlendi (archive veya durum güncel)
- [ ] 4 kritik bugfix tamam + test geçti
- [ ] DailyReminderJob çalışıyor (Hangfire dashboard'da görünür)
- [ ] Plan 22 Holidays implement (Calendar'da görünür, view filter çalışır)
- [ ] Notification cross-modül (Obligation due → push, Tamim hatırlatma)
- [ ] AI Wizard end-to-end smoke (upload → start → poll → form doldur → kaydet)
- [ ] ContractsController 3 partial dosya, hard-limit altında
- [ ] ObligationsController.Edit çalışır
- [ ] Plan 27 Faz A %100 (Validator + Stage 3 + 10 prompt)
- [ ] Plan 25.1 tamam (Faz 5 queue + Faz 6 smoke)
- [ ] ADR-016 + ADR-017 yazılı
- [ ] **Test:** `dotnet test` 337+ → 0 başarısız (her bugfix sonrası kontrol)
- [ ] Faz 4 (D2 minimum) tamamlandı veya SOP'a kaydırma kararı verildi
- [ ] Journal ve memory güncel

---

## 8. Rollback Planı

Her bugfix ve modül tamamlama **ayrı commit** — istenmeyen davranış `git revert <commit>` ile geri alınır.

DailyReminderJob için kapatma: `Hangfire dashboard /hangfire` → recurring jobs → trigger off. Plan 22 entity'leri için: migration 58-60 rollback script gerekli (DROP TABLE + DROP VIEW idempotent).

---

## 9. Adımlar (TODO.md ile senkron)

### Faz 1 (bugün/yarın, 7-10 saat)
1. [ ] **R-01** Plan 16.6 archive
2. [ ] **R-02** Plan 17 done criteria + archive + Plan 17.1 mini
3. [ ] **R-03** Plan 20 archive
4. [ ] **R-04** Plan 21 durum güncelle
5. [ ] **R-05** Plan 23 archive
6. [ ] **R-06** Plan 25 + 25.1 + 27 durum güncelle
7. [ ] **R-07** Plan 26 SUPERSEDED archive
8. [ ] **B-01** AiController.cs:446 fix + test
9. [ ] **B-02** Review.cshtml:126 fix + smoke
10. [ ] **B-03** DocumentsController App_Data + Download endpoint + test
11. [ ] **B-04** WizardExtractionService Tesseract chain + test

### Faz 2 (1-1.5 hafta, 37-50 saat)
12. [ ] **C-01** DailyReminderJob
13. [ ] **C-02** Plan 22 Holidays (ADR-016 önce)
14. [ ] **C-03** Notification cross-modül caller'lar
15. [ ] **C-04** AI Wizard Faz 1 (BUGFIX-4 üzerine)
16. [ ] **C-05** ContractsController split
17. [ ] **C-06** ObligationsController.Edit
18. [ ] **C-07** Plan 27 Faz A eksikleri
19. [ ] **C-08** Plan 25.1 Faz 5+6

### Faz 3 (ayrı oturum, 4-6 saat)
20. [ ] **A-016** ADR-016 Calendar unified
21. [ ] **A-017** ADR-017 Compliance scope
22. [ ] **V-01** VISION AI Generation karar notu

### Faz 4 (opsiyonel)
23. [ ] **D-01** D2: AI iyileştirme (önerilen)
24. [ ] **D-02..05** D1+D3+D4+D5 kararla

> TODO.md'ye Faz 1 + Faz 2 maddeleri eklenecek, EN ÜST ÖNCELİK olarak (SOP'tan önce).

---

## 10. İlişkili

- **Yön belgesi:** [`docs/VISION.md`](../docs/VISION.md) §2-§3 olgun/yarım modül listesi
- **ADR'ler:** ADR-002 (modular monolit), ADR-013 (multi-DB), ADR-014 (frontend), ADR-015 (yeni modül assembly)
- **Yeni ADR'ler:** ADR-016 (Calendar unified, Faz 3), ADR-017 (Compliance scope, Faz 3)
- **Plan referansları:** Plan 22 (Holidays, C2'de açar), Plan 25 + 25.1 + 27 (durum güncelle), Plan 31 (SMTP caller C1'de), Plan 32 (Scheduled Reports C3'te)
- **Sonraki planlar:** Plan 34 (SOP) — bu plan bittikten sonra Faz A başlar
- **Skill:** `vnext-entity-port` (Plan 22 ve Plan 34 için ortak şablon)
- **Test disiplini:** [`.claude/rules/test-discipline.md`](../.claude/rules/test-discipline.md) — her bugfix ve fazda `dotnet test` zorunlu

---

## 11. Onay

- [x] Plan kullanıcıya gösterildi: 2026-05-14 (konsolide rapor + 4 öncelik bandı)
- [x] Geri bildirim: "bunları detaylı işle" — varsayılan kabul, tüm 4 faz onaylandı
- [x] **Onaylandı:** 2026-05-14. Faz 1 hemen başlar.

> Değişiklik gerekirse plan dosyası önce güncellenir, sonra implementation devam eder.
