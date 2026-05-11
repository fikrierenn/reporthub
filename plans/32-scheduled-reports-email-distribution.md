# Plan 32 — Scheduled Reports + Email Distribution

**Tarih:** 2026-05-12
**Yazan:** Claude
**Durum:** `Taslak — kullanıcı onayı bekliyor`

---

## 1. Problem

Mosaik raporları **manuel çalıştırılır**: kullanıcı her seferinde `/Reports/Run/{id}` aç, parametre gir, `Çalıştır`, `Export Excel`. Sık tekrarlanan raporlarda (aylık PDKS özet, haftalık satış, günlük açık yükümlülük) bu döngü:

- **Tekrarlayıcı manuel iş** — operasyonel yük
- **Kaçırılan deadline** — kullanıcı raporu çıkarmayı unutursa yöneticiler veriyi göremez
- **Dağıtım yok** — rapor sadece çıkaran kişide kalır; ilgili kullanıcılara e-posta ile iletmek için ayrı bir adım

Plan 31 ile `IEmailService` altyapısı geldi (`Mosaik.Core.Email`). Hangfire kuruluyor (`Program.cs`, `AiExtractionWorker`). İki parça birleşip **otomatik rapor üreten + dağıtan zamanlama sistemi** kurulabilir.

## 2. Scope

### Kapsam dahili
- `ReportSchedule` entity + tablo (`Migration 57`):
  - `Id`, `ReportId` (FK `ReportCatalog`), `Name`, `CronExpression`, `ParametersJson`, `IsEnabled`,
  - `Format` enum (`Excel`, `Pdf`, `HtmlInline`),
  - `CreatedBy`, `CreatedAt`, `LastRunAt`, `LastRunStatus`, `NextRunAt`
- `ReportScheduleRecipient` join table — `ScheduleId` × `UserId` (Mosaik users) + opsiyonel `ExternalEmail` (DB-dışı alıcı)
- `ReportRunHistory` entity — her execution: status, attachment path, error, sent count, duration
- `IScheduledReportService`:
  - `CreateAsync(ReportScheduleCreateVm)`, `UpdateAsync`, `DeleteAsync`, `ToggleAsync`
  - `ExecuteNowAsync(scheduleId)` — manuel tetik
- `ScheduledReportRunner` (Hangfire `RecurringJob`):
  - SP execute (`IStoredProcedureExecutor`) → multi-RS DataTable
  - Excel: `ClosedXMLExportService` (mevcut) → tek workbook, her RS ayrı sheet
  - PDF: `iText 7` veya `PuppeteerSharp` Razor view → PDF (faz 2'ye ertelenebilir)
  - HTML Inline: rapor sonucu HTML body'ye gömülür (küçük tablolar için)
  - `IEmailService.SendAsync` (attachment desteği eklenecek — `EmailAttachment(stream, fileName, mimeType)`)
- `IEmailService` imza genişletme: `Task SendAsync(string to, string subject, string html, IEnumerable<EmailAttachment>? attachments = null, CancellationToken ct)`
- `EmailTemplates.ScheduledReport(reportName, runDate, summary, hasAttachment)` — yeni template
- Admin UI: `/Admin/ScheduledReports`:
  - Liste (cron, son çalışma, sonraki çalışma, alıcı sayısı, durum toggle)
  - Create/Edit ekranı: rapor seçimi (autocomplete), parametre formu (rapor ParamSchema'sından dinamik), cron picker, alıcı çoklu select (Mosaik users + manuel email), format radio
  - Test Run butonu — şimdi çalıştır + son hata göster
- Audit log: schedule create/update/delete/manual-trigger
- Sidebar: Sistem grubu altına "Zamanlanmış Raporlar" link (İzleme alt-section'a `/Admin/ScheduledReports`)
- Cron picker UI: 7 hazır preset (her gün 08:00 / her hafta Pzt 09:00 / her ayın 1'i / vb) + manuel cron string
- Hangfire RecurringJob registration: app startup'ta DB'deki schedule'lar `RecurringJob.AddOrUpdate` ile kayıt

### Kapsam dışı (sonraki fazlara bırakılır)
- PDF format (Faz 2) — Excel + HTMLInline ile başla, PDF library seçimi ayrı tartışma
- Conditional triggers — "obligation > 100K oluştuğunda mail at" (Plan 33 candidate)
- Per-recipient parameter override — herkese aynı parametre + sonuç gider
- Excel'de pivot/grafik — düz tablo (mevcut ExcelExportService davranışı)
- HTML email içinde chart image — Chart.js server-render karmaşık, ileri sprint
- Dynamic recipient list (SP'den dön) — sabit liste seç
- Retry policy — Hangfire default retry yeterli (3x)
- SMS / Webhook / Teams kanalı — sadece email
- Multi-language template — sadece TR
- Schedule kopyalama / template'leme — gerek olunca

### Etkilenen dosyalar
- `Mosaik/Database/57_CreateReportSchedule.sql` (yeni)
- `Mosaik/Models/ReportSchedule.cs`, `ReportScheduleRecipient.cs`, `ReportRunHistory.cs` (yeni)
- `Mosaik/Models/ReportPanelContext.cs` (DbSet + relationship config)
- `Mosaik/Services/ScheduledReportService.cs` (yeni — `IScheduledReportService`)
- `Mosaik/Services/ScheduledReportRunner.cs` (yeni — Hangfire job)
- `Mosaik.Core/Email/IEmailService.cs` (attachment parametresi) + `EmailAttachment` record
- `Mosaik/Services/Email/SmtpEmailService.cs` (`MailMessage.Attachments.Add(Attachment)`)
- `Mosaik/Services/Email/EmailTemplates.cs` (`ScheduledReport` template)
- `Mosaik/Controllers/AdminController.ScheduledReports.cs` (yeni partial)
- `Mosaik/Views/Admin/ScheduledReports.cshtml`, `CreateScheduledReport.cshtml`, `EditScheduledReport.cshtml` (yeni)
- `Mosaik/ViewModels/ScheduledReportFormViewModel.cs` (yeni)
- `Mosaik/wwwroot/assets/js/scheduled-report-form.js` (yeni — cron preset + recipient picker)
- `Mosaik/Program.cs` — DI kayıt + startup'ta `RecurringJob.AddOrUpdate` loop
- `Mosaik/Views/Shared/_AppLayout.cshtml` — Sistem grubu İzleme alt-section'a yeni link
- `Mosaik/Views/Admin/_AdminSubnav.cshtml` — opsiyonel: Sistem grubuna kart

**Tahmini boyut:** 14 dosya / ~900 satır kod + ~150 satır SQL. Tier 3.

## 3. Alternatifler

### A: Zamanlama yok, sadece "Şimdi e-posta at" butonu
**Açıklama:** `/Reports/Run/{id}` sayfasına Excel export + e-posta at butonu eklenir, zamanlama olmaz.
**Reddetme sebebi:** Tekrarlayıcı işleri çözmez. Kullanıcı hâlâ her seferinde tetikler.

### B: Hangfire + RecurringJob (SEÇİLEN)
**Açıklama:** Mevcut Hangfire altyapısı + DB-driven schedule kayıtları + uygulama açılışta RecurringJob register.
**Sebep:** Hangfire zaten projede (`AiExtractionWorker`). Mature, dashboard'u var (`/hangfire` route eklendi). Cron syntax standart.

### C: Quartz.NET
**Açıklama:** Daha güçlü zamanlama (calendar exclusions, misfire policy) — ayrı paket.
**Reddetme sebebi:** Hangfire varken ikinci scheduler dependency artırır. Mosaik'in karmaşıklık ihtiyacı bunu gerektirmiyor.

### D: Crontab / Windows Task Scheduler + standalone executable
**Açıklama:** Konsol app yaz, OS scheduler tetikler.
**Reddetme sebebi:** Deployment karmaşıklaşır (ayrı binary), audit yapmak zor, kullanıcı UI'dan kontrol edemez.

### E: Cron string yerine "Aylık 1'inde 09:00" gibi yapılandırılmış picker, cron generate
**Açıklama:** UI yalnızca preset + custom builder, cron string DB'de tutulur ama kullanıcıya gizlenir.
**Sebep:** Plan 32 hem preset hem manuel cron destekler — admin teknik biri için manuel cron, non-teknik için preset. İki şıkkı da destekle.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| SMTP `Enabled=false` durumda schedule sessizce skip eder — kullanıcı fark etmez | yüksek | yüksek | `ExecuteNowAsync` butonu kullanıcıya `IsEnabled=false` durumunda UI'da uyarı verir + `ReportRunHistory.Status="skipped_no_smtp"` |
| Büyük rapor (>10MB Excel) — email server reject | orta | orta | `appsettings`'e `MaxAttachmentSizeMb` limit + aşılırsa "rapor için linkten indirin" mesajı (link `/Admin/ScheduledReports/Download/{historyId}`) |
| Cron yanlış girilirse infinite loop / sürekli tetik | yüksek | düşük | UI cron validator (sub-minute reddet) + 60s minimum interval guard |
| ReportCatalog'tan rapor silinince schedule orphan | orta | düşük | FK CASCADE değil, RESTRICT — silinmeden önce "X zamanlama bu raporu kullanıyor" uyar |
| `ParametersJson` rapor `ParamSchema`'sıyla uyumsuzlaşır (rapor parametre eklenince eski schedule kırılır) | orta | yüksek | `ScheduledReportRunner` SP execute öncesi parametre validate eder, hata varsa `LastRunStatus="param_mismatch"` + admin'e bildirim |
| Recipient listesi büyürse (>100 alıcı) tek seferde SendBulkAsync timeout | orta | düşük | Batch (10'arlı) + her batch arası `await Task.Delay(500)` |
| Hangfire job çalışırken DB locked → retry storm | düşük | düşük | Hangfire built-in retry policy (3x backoff) + transaction scope dar |
| `RecurringJob.AddOrUpdate` startup'ta DB schedule sayısı × app instance sayısı duplicate kayıt | orta | düşük | `IServer.UnregisterAll` startup'ta + idempotent `AddOrUpdate` (Hangfire bunu zaten yapıyor) |
| Test ortamında recipient gerçek email'lere mail atar | yüksek | orta | `appsettings:SmtpSettings:TestModeRecipient` — set ise tüm gönderim bu adrese yönlendirilir (alıcı listesi log'a yazılır) |
| Inline style yasağı ihlali (yeni view) | düşük | orta | `ui-patterns.md` + `inline-style-guard.md` zorunlu, refactor tamamlanana kadar yeni view'da inline style sıfır |

## 5. Done Criteria

- [ ] Migration 57 uygulanır, 3 yeni tablo (`ReportSchedules`, `ReportScheduleRecipients`, `ReportRunHistory`) gelir
- [ ] `IEmailService.SendAsync(..., attachments)` genişletilir, `EmailAttachment(Stream, string, string)` record
- [ ] `/Admin/ScheduledReports` liste sayfası — cron, son çalışma, sonraki çalışma, recipient sayısı, toggle
- [ ] `/Admin/ScheduledReports/Create` formu — rapor seçimi + parametre formu (rapor ParamSchema'sından dinamik render) + cron preset/custom + recipient picker (Mosaik user search + manuel email)
- [ ] `Hangfire RecurringJob` her schedule için kayıt — uygulama yeniden başlatıldığında DB'den yenilenir
- [ ] `IsEnabled=false` schedule cron tetiklenmez
- [ ] "Test Run" butonu — manuel `BackgroundJob.Enqueue(...)` ile şimdi çalıştır
- [ ] `ReportRunHistory` her execution'da satır yazar — durum + boyut + alıcı sayısı + hata mesajı
- [ ] Excel format çalışır — multi-RS rapor her sheet ayrı
- [ ] Email gönderim: ek olarak Excel, body'de rapor adı + tarih + alıcı listesi + (opsiyonel) HTML inline özet
- [ ] Audit log: schedule create/update/delete/manual-trigger
- [ ] SMTP disabled durumda execution `LastRunStatus="skipped_no_smtp"` + admin notification
- [ ] Sidebar Sistem > İzleme'de "Zamanlanmış Raporlar" link
- [ ] PDF format → **Faz 2'ye ertelenir** (Plan 32.1)

## 6. Rollback Planı

- Git revert (3-4 commit beklenir — entity / service / UI / sidebar)
- Migration 57 down: `DROP TABLE ReportRunHistory; DROP TABLE ReportScheduleRecipients; DROP TABLE ReportSchedules;`
- `RecurringJob.RemoveIfExists` ile registered job'ları temizle
- Sidebar link kaldır
- Eğer prod'a deploy ettiyse aktif schedule'ları `IsEnabled=0`'a çek (data kaybetme) → rollback sonrası tekrar açılır

## 7. Adımlar

1. [ ] **Faz 1 — Şema + Entity**
   - Migration 57 (3 tablo + FK + index)
   - 3 entity dosyası + DbSet + relationship config
   - Build temiz, migration apply
2. [ ] **Faz 2 — IEmailService genişletme**
   - `EmailAttachment` record
   - `SendAsync(...)` overload (geriye uyumlu — default `null`)
   - `SmtpEmailService` attachment handling
   - `EmailTemplates.ScheduledReport(...)`
3. [ ] **Faz 3 — Runner servisi**
   - `IScheduledReportService` interface + impl (CRUD + ExecuteNow)
   - `ScheduledReportRunner` Hangfire job — SP execute → Excel → email
   - Program.cs startup'ta DB'den `RecurringJob.AddOrUpdate` loop
4. [ ] **Faz 4 — Admin UI**
   - `AdminController.ScheduledReports.cs` (List/Create/Edit/Delete/Toggle/TestRun)
   - 3 view (ScheduledReports, Create, Edit) — `ui-patterns.md` + inline-style yasak
   - `scheduled-report-form.js` — cron preset, recipient autocomplete (Mosaik users)
   - `_AdminSubnav.cshtml` Sistem altına link
   - Sidebar `_AppLayout.cshtml` Sistem > İzleme'ye link
5. [ ] **Faz 5 — Test + audit + edge cases**
   - Manuel Test Run → mail geldi mi
   - `IsEnabled=false` → skip
   - Big report (>10MB) → reject + link fallback
   - Recipient list >100 → batch
   - SMTP disabled → graceful skip + history record
   - ParamSchema değişimi → `param_mismatch` history
6. [ ] **Faz 6 — Plan 32.1 (PDF format) ayrı plan**

## 8. İlişkili

- **Plan 31** (Email/SMTP) — `IEmailService` altyapısı, bu plan attachment desteğini ekler
- **Plan 23** (Sidebar) — Sistem grubu altına yeni link
- **Plan 16.5 Faz C** (`IUserDataScope`) — schedule execution sırasında recipient firma'sına göre data filter? **HAYIR** — schedule oluşturanın yetkisi geçerli, recipient'a göre filter yapılmaz (riskli). Sadece schedule sahibi rapor erişimi olan birisinin olduğu doğrulanır.
- **ADR-007** (Hangfire kullanım kalıbı) — referans
- **ADR yazılacak:** `docs/ADR/013-scheduled-reports.md` — neden Hangfire B alternatifi, neden PDF Faz 2

## 9. Açık Sorular (onay öncesi)

1. **PDF format gerçekten ileri faza ertelensin mi?** Excel + HTMLInline başlangıçta yeter mi? (kullanıcı kararı)
2. **External email recipient'lar** Mosaik user değil — bunlar nasıl yetkilendirilir? (sadece admin schedule oluşturabilsin → external email serbest mi yoksa whitelist gerekli mi?)
3. **Cron picker:** sadece preset mi, preset + custom string mi, third-party JS lib (cron-picker) mi?
4. **`MaxAttachmentSizeMb` default kaç MB?** SMTP server'lar tipik 10-25 MB. 10 öneriyorum.
5. **Schedule sahibi pasif olursa ne olur?** (`IsActive=0` user'ın schedule'ları otomatik durdurulsun mu, yoksa schedule kayıtlı kalsın mı?) — Önerim: durdurulsun, audit log'a kaydet.
6. **Test recipient mode** — SMTP Settings'e `TestModeRecipient` ekleyelim mi? (dev/staging'de yanlışlıkla gerçek email'lere gitmesin)

## 10. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Soru 1-6 cevaplandı
- [ ] Onay alındı: ___
- [ ] Implementation başladı (Faz 1)
