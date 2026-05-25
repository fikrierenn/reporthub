# DikkatIQ → Mosaik Adaptasyon Kılavuzu

**Kaynak:** `D:/Dev/DikkatIQ`  
**Tarih:** 2026-05-25  
**Amaç:** DikkatIQ'dan Mosaik vNext'e port edilecek bileşenlerin referans kılavuzu.

---

## 1. FallbackLlmService — 5 Provider Chain

**Kaynak:** `DikkatIQ/src/DikkatIQ.Infrastructure/Services/FallbackLlmService.cs`  
**Interface:** `DikkatIQ/src/DikkatIQ.Core/Interfaces/ILlmService.cs`

### Yapı

5 provider sıralı fallback: **Ollama → Gemini → Claude → OpenAI → OpenRouter**  
`PrimaryProvider` config'den okunur, geri kalanlar sırayla denenir.

`LlmResponse` record:
```csharp
record LlmResponse(
    bool Success,
    string Content,
    string ModelUsed,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCostUsd,
    string? ErrorMessage
);
```

### Mosaik durumu

Mosaik Tamim/Contracts AI: 3 provider (Groq/z.ai/Grok/Gemini) — provider-specific kod, generic chain yok.  
Plan 34.1 SOP: LLamaSharp in-process (ADR-022) — Ollama HTTP değil, in-process.

### Mosaik entegrasyon hedefi

**D-02 + D-03 AI altyapı iyileştirme:**

`Mosaik.Core.AI/Services/FallbackLlmService.cs` olarak port et. Değişiklikler:
- Provider chain: **LlamaSharp (in-process)** → **Ollama HTTP** → **Groq** → **Gemini** → **Claude**
- `EstimatedCostUsd` → in-process için 0, Groq/Gemini için hesapla
- `LlmResponse` → Mosaik.Core.AI'daki `IAiResponse` ile hizala

```csharp
// Mosaik.Core.AI/Interfaces/ILlmService.cs
public interface ILlmService
{
    Task<LlmResult> GenerateAsync(LlmRequest request, CancellationToken ct = default);
}

public record LlmResult(
    bool Success,
    string Content,
    string ProviderUsed,
    int InputTokens,
    int OutputTokens,
    string? Error = null
);
```

---

## 2. PdfPigTextExtractor — 2 Aşamalı PDF+OCR Pipeline

**Kaynak:** `DikkatIQ/src/DikkatIQ.Infrastructure/Services/PdfPigTextExtractor.cs`  
**Config:** `DikkatIQ/src/DikkatIQ.Core/Settings/OcrSettings.cs`

### Pipeline

```
Faz 1: PdfPig direkt text extraction (sayfa başına 20 karakter eşiği)
  → eşik altında kalırsa →
Faz 2: OCRmyPDF CLI (--deskew --rotate-pages --force-ocr, tur+eng)
  → timeout veya hata →
Fallback: Faz 1 sonucu döner (exception yutulmaz, Success=false)
```

Güvenlik: `MaxCharacters=500_000` ile LLM context overflow guard.  
Process kapatma: `process.Kill(entireProcessTree: true)` + timeout.

### Mosaik durumu

Plan 27 PDF extraction PdfPig kullanıyor ama OCR fallback yok, eşik mantığı yok.

### Mosaik entegrasyon hedefi

**Plan 27 Faz C veya D-03:**

`OcrSettings` → `Mosaik/appsettings.json`:
```json
"Ocr": {
  "OcrMyPdfPath": "ocrmypdf",
  "Language": "tur+eng",
  "TimeoutSeconds": 120,
  "ExtraArgs": "--deskew --rotate-pages"
}
```

`PdfPigTextExtractor` → `Mosaik/Services/Documents/PdfTextExtractor.cs` olarak port et.  
20-karakter eşiği + OCR fallback Plan 27'deki Tesseract vision rescue'yu tamamlar.

---

## 3. ExtractionPrompts — TR OCR-Tolerant Prompt'lar

**Kaynak:** `DikkatIQ/src/DikkatIQ.Infrastructure/Prompts/ExtractionPrompts.cs`

### İçerik

- `Stage1SystemPrompt` (65 satır): OCR bozukluğu toleransı, Türkçe karakter normalizasyonu, taraf/tarih/tutar kriterleri
- `GetStage2SystemPrompt(category)`: Lease/Service/Supply/Employment/License/Insurance için 8-10 madde derinleştirme
- `JsonSchema`: tam JSON Schema draft-07

### Mosaik entegrasyon hedefi

**Plan 27 Faz B (AI Wizard) prompt hardening:**

Mosaik'te eksik olan kısım: OCR bozukluğu toleransı (`"karakter bozulması olabilir"` notu) ve Türkçe karakter normalizasyon talimatı.  
Doğrudan `Mosaik/Services/Ai/ExtractionPromptBuilder.cs`'e merge edilebilir.

---

## 4. Notification Entity + Service

**Kaynak:**  
- `DikkatIQ/src/DikkatIQ.Core/Entities/Notification.cs`  
- `DikkatIQ/src/DikkatIQ.Infrastructure/Services/NotificationService.cs`

### Yapı

```csharp
// Polymorphic notification — Plan 42 için hazır referans
class Notification {
    int Id;
    int UserId;
    string Title;
    string Body;
    string EntityType;   // "ProcessInstance" / "SopVersion" / "Obligation" ...
    int EntityId;
    NotificationChannel Channel;  // InApp / Email
    bool IsRead;
    DateTime? ReadAt;
    DateTime SentAt;
}
```

### Mosaik entegrasyon hedefi

**Plan 42 ProcessExecution Faz 4 (SLA + Bildirim):**

`EntityType/EntityId` polymorphic pattern Mosaik `ProcessInstance`, `SopVersion`, `Obligation`, `Circular` için doğrudan kullanılabilir.  
`NotificationService.GetUnreadCountAsync` → sidebar badge için.  
`AiExtractionService.CreateRiskNotificationsAsync` pattern (company admin'lara toplu bildirim) → Plan 47 Auto-Tuning uyarıları için.

Migration örneği:
```sql
CREATE TABLE Notifications (
    Id          INT IDENTITY PRIMARY KEY,
    UserId      INT NOT NULL,
    Title       NVARCHAR(200) NOT NULL,
    Body        NVARCHAR(2000),
    EntityType  NVARCHAR(50),
    EntityId    INT,
    IsRead      BIT NOT NULL DEFAULT 0,
    ReadAt      DATETIME2,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)
```

---

## 5. SmtpEmailService + EmailTemplates

**Kaynak:**  
- `DikkatIQ/src/DikkatIQ.Infrastructure/Services/SmtpEmailService.cs`  
- `DikkatIQ/src/DikkatIQ.Infrastructure/Services/EmailTemplates.cs`

### Özellikler

- `IEmailService`: `SendAsync` + `SendBulkAsync`
- `IsEnabled` guard — config eksikse sessizce skip
- Template'ler: `OverdueObligation`, `ObligationReminder`, `DailyDigest`, `AiExtractionComplete`

### Mosaik entegrasyon hedefi

**Plan 32 SMTP altyapısı:**  
DikkatIQ'nun `SmtpEmailService` Mosaik'e doğrudan port edilebilir. `IsEnabled` guard pattern kritik (dev ortamında SMTP olmadan crash önler). Template'ler Mosaik brand (`--clr-primary`) ile yeniden renklendirilecek.

---

## 6. RecurrenceService — Rolling Date Generator

**Kaynak:** `DikkatIQ/src/DikkatIQ.Infrastructure/Services/RecurrenceService.cs`  
**Factory:** `DikkatIQ/src/DikkatIQ.Core/Helpers/ObligationFactory.cs`

### Özellikler

- `GenerateDueDatesInRange`: Monthly/Quarterly/Yearly + `DayOfMonth` pin + ay sonu clip
- `GenerateMonthlyRollingAsync`: 12 ay horizon, `MaxOccurrences` limiti, duplicate guard
- `ObligationFactory.CreateChild`: parent-child obligation ağacı

### Mosaik durumu

Mosaik `ComplianceDueCalculator` var ama rolling window generator yok (Plan 25 Faz B eager producer eksik).

### Mosaik entegrasyon hedefi

**Plan 25 Faz B / Plan 42 Faz 6:**  
`RecurrenceService.GenerateMonthlyRollingAsync` → `Mosaik.Modules.Compliance/Services/ObligationRollingGenerator.cs`  
`MaxOccurrences` guard + duplicate check Mosaik'in Hangfire `DailyReminderJob`'ına entegre edilebilir.

---

## 7. ICurrentCompanyService — Claim-Based Multi-Firma

**Kaynak:**  
- `DikkatIQ/src/DikkatIQ.Core/Interfaces/ICurrentCompanyService.cs`  
- `DikkatIQ/src/DikkatIQ.Infrastructure/Services/CurrentCompanyService.cs`

### Yapı

Cookie claim'den `"CompanyId"` okuma. Tüm servisler DI üzerinden alır, HttpContext doğrudan erişim yok.

### Mosaik durumu

Mosaik `UserDataFilter` + `UserDataFilterInjector` kullanıyor — claim-based değil, DB-driven filter.

### Mosaik entegrasyon hedefi

DikkatIQ yaklaşımı daha temiz ama Mosaik'in mevcut filter mekanizması değiştirilmeyecek (Plan 14 kararı). Ancak `ICurrentCompanyService` pattern'i Plan 16.5 `IUserDataScope` güncellemesine referans alınabilir.

---

## Öncelik Sırası

| # | Bileşen | Hedef Plan | Effort | Öncelik |
|---|---|---|---|---|
| 1 | FallbackLlmService chain | D-02 + D-03 | 4-6h | YÜKSEK |
| 2 | PdfPigTextExtractor OCR fallback | Plan 27 Faz C | 3-4h | YÜKSEK |
| 3 | Notification entity + service | Plan 42 Faz 4 | 3-5h | ORTA |
| 4 | ExtractionPrompts TR OCR-tolerant | Plan 27 Faz B | 2h | ORTA |
| 5 | SmtpEmailService port | Plan 32 | 3-4h | ORTA |
| 6 | RecurrenceService rolling generator | Plan 42 Faz 6 | 4-6h | ORTA |
| 7 | ICurrentCompanyService | Plan 16.5 referans | 0h (referans) | DÜŞÜK |
