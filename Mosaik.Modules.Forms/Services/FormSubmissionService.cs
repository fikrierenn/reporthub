using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Core.Notification;
using Mosaik.Core.Workflow;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    public sealed record FormSubmissionInput(
        int FormDefinitionId,
        int FirmaId,
        IReadOnlyDictionary<string, string?> Values,
        int? SubmittedById,
        string? SubmitterEmail,
        string? SubmitterPhone,
        string? SubmitterIp,
        string? SubmitterUserAgent,
        int? PublicTokenId);

    // Plan 41 Faz 1 — submission save + FormVersionId bind (§4.6 ZORUNLU). Workflow/ProcessInstance
    // trigger CANLI (Plan 57 B1 — TriggersWorkflowId ise IWorkflowService.StartAsync; stale-stub yorumu düzeltildi).
    // Faz 4 — File/Signature alanları: base64 dataURL decode → magic-byte doğrula → disk yaz → ValueFileId.
    public class FormSubmissionService(DbContext db, FormValidationService validation, FormFileStorage fileStorage, FormEncryptionService encryption, INotificationService notifications, IWorkflowService workflow, IAuditLog audit, ILogger<FormSubmissionService> logger)
    {
        // silent-failure-hunter HIGH — alan-bazlı hata dict'i bu koda taşınır (JSON-encoded
        // Message); controller/JS ayırt edip survey-core question.addError'a bağlar.
        public const string FieldValidationErrorCode = "field_validation";

        public async Task<ServiceResult<int>> SubmitAsync(FormSubmissionInput input, CancellationToken ct = default)
        {
            var def = await db.Set<FormDefinition>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == input.FormDefinitionId && d.FirmaId == input.FirmaId && d.Status == 1, ct);
            if (def == null)
                return ServiceResult<int>.Failure("Form bulunamadı veya yayında değil.");

            var latestVersion = await db.Set<FormDefinitionVersion>().AsNoTracking()
                .Where(v => v.FormDefinitionId == def.Id)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync(ct);
            if (latestVersion == null)
                return ServiceResult<int>.Failure("Form yayınlanmış ama sürüm kaydı yok — tutarsız durum.");

            var errors = await validation.ValidateAsync(def.Id, input.Values, ct);
            if (errors.Count > 0)
                return ServiceResult<int>.Failure(JsonSerializer.Serialize(errors), FieldValidationErrorCode);

            // Login VEYA geçerli public token yoksa reddet. Public token (PublicTokenId) kimlik
            // doğrulama yerine geçer — public form IsAnonymous olmasa da token ile submit kabul.
            if (!def.IsAnonymous && input.SubmittedById == null && input.PublicTokenId == null)
                return ServiceResult<int>.Failure("Bu form anonim submit'e izin vermiyor, giriş yapmalısınız.");

            var fields = await db.Set<FormField>().AsNoTracking()
                .Where(f => f.FormDefinitionId == def.Id)
                .ToListAsync(ct);

            // Public/anonim (token ile veya IsAnonymous) daha sıkı boyut limiti (advisor conf 80).
            // Şifreli form da sıkı limit (security M-1): decrypt DataProtection'da streaming yok →
            // indirmede tam plaintext bellekte buffer'lanır; 10MB cap LOH baskısını sınırlar.
            var maxBytes = def.IsAnonymous || def.IsEncrypted || input.PublicTokenId != null
                ? FormFileStorage.PublicMaxBytes
                : FormFileStorage.InternalMaxBytes;

            // Pass 1: File/Signature alanlarını DECODE et (disk'e dokunmadan) — bir alan geçersizse
            // hiçbir dosya yazılmadan field_validation döner (orphan dosya bırakma).
            var decoded = new List<(FormField Field, FormFileStorage.DecodedFile File)>();
            var fileErrors = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                if (field.FieldType is not (FormFieldType.File or FormFieldType.Signature))
                    continue;
                // G3 AUTHORITATIVE: koşulu sağlanmayan (gizli) alanın dosyası diske yazılmaz/saklanmaz —
                // kötü client gizli alana değer basamaz (anonimlik/koşul bütünlüğü server'da zorlanır).
                if (!FormConditionEvaluator.IsVisible(field, input.Values))
                    continue;
                if (!input.Values.TryGetValue(field.FieldKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                    continue;

                var r = field.FieldType == FormFieldType.Signature
                    ? fileStorage.DecodeSignature(raw, maxBytes)
                    : fileStorage.DecodeFileField(raw, maxBytes);
                if (!r.IsSuccess)
                    fileErrors[field.FieldKey] = r.Message;
                else
                    decoded.Add((field, r.Data));
            }
            if (fileErrors.Count > 0)
                return ServiceResult<int>.Failure(JsonSerializer.Serialize(fileErrors), FieldValidationErrorCode);

            var submission = new FormSubmission
            {
                FormDefinitionId = def.Id,
                FirmaId = input.FirmaId,
                FormVersionId = latestVersion.Id,
                SubmittedById = input.SubmittedById,
                SubmitterEmail = input.SubmitterEmail,
                SubmitterPhone = input.SubmitterPhone,
                SubmitterIp = input.SubmitterIp,
                SubmitterUserAgent = input.SubmitterUserAgent,
                PublicTokenId = input.PublicTokenId,
                Status = 0
            };

            // Pass 2a: dosya olmayan alanlar.
            foreach (var field in fields)
            {
                if (field.FieldType is FormFieldType.Hidden or FormFieldType.Section)
                    continue;
                if (field.FieldType is FormFieldType.File or FormFieldType.Signature)
                    continue;
                // G3 AUTHORITATIVE: gizli alanın değeri saklanmaz (validation zaten atlar; burada da
                // persist edilmez — aksi halde koşullu-gizli alana basılan değer sızardı).
                if (!FormConditionEvaluator.IsVisible(field, input.Values))
                {
                    // Gizli alana gönderilen dolu değer atılıyor = tamper/client-drift sinyali — iz bırak.
                    if (input.Values.TryGetValue(field.FieldKey, out var dropped) && !string.IsNullOrWhiteSpace(dropped))
                        logger.LogWarning("Forms: gizli alana gönderilen değer atıldı. form={FormId} field={FieldKey}", def.Id, field.FieldKey);
                    continue;
                }
                if (!input.Values.TryGetValue(field.FieldKey, out var raw) || raw == null)
                    continue;

                submission.Values.Add(BuildFieldValue(field, raw, def.IsEncrypted));
            }

            // Pass 2b: decode edilmiş dosyaları diske yaz + submission graph'ına bağla + kaydet.
            // Write loop + SaveChanges TEK try içinde (silent-failure F3): mid-batch WriteToDisk IOException'ı
            // da orphan temizliğine dahil olur — yazılmış dosyalar `written`'da, catch hepsini siler.
            var written = new List<FormSubmissionFile>();
            try
            {
                foreach (var (field, file) in decoded)
                {
                    // A2-#1: şifreli formda dosya ekleri de şifreli yazılır (alan-şifrelemeyle tutarlı).
                    var stored = await fileStorage.WriteToDiskAsync(file, input.FirmaId, field.FieldKey, def.IsEncrypted, ct);
                    written.Add(stored);
                    submission.Files.Add(stored);
                    submission.Values.Add(new FormSubmissionFieldValue
                    {
                        FormFieldId = field.Id,
                        FieldKey = field.FieldKey,
                        File = stored
                    });
                }

                db.Set<FormSubmission>().Add(submission);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception)
            {
                fileStorage.TryDeleteAll(written); // disk-write veya DB başarısız — yazılmış dosyaları temizle
                throw;
            }

            // Plan 56 M-A G4 — submit sonrası bildirim: form sahibine (def.CreatedBy) in-app, BEST-EFFORT.
            // Submission zaten commit'li (SaveChanges yukarıda); bildirim ayrı try/catch — fail'de submit
            // BAŞARILI kalır (non-critical, kullanıcıya submit hatası yansımaz). Orphan-file catch'inin
            // DIŞINDA (o blok rollback semantiği taşır). İçerik METADATA-ONLY (message=null): şifreli/anonim
            // form değeri bildirime SIZMAZ (KVKK + tutarlılık); detay yetki-gated ekranda. Recipient hep
            // owner (personel) — submitter DEĞİL. CreatedBy=0 (eski/seed form) guard.
            if (def.CreatedBy > 0)
            {
                try
                {
                    // Notification.Title MaxLength(200); def.Name (≤200) + sabit son-ek başlığı taşırsa
                    // CreateAsync SaveChanges throw eder → uzun-adlı formda bildirim sessizce düşerdi. Kırp.
                    var shortName = def.Name.Length > 160 ? def.Name[..160] + "…" : def.Name;
                    await notifications.CreateAsync(
                        def.CreatedBy, "form_submission", submission.Id,
                        $"'{shortName}' formuna yeni yanıt geldi", null,
                        $"/Forms/FormDefinition/SubmissionDetail/{submission.Id}", "info", "system");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Forms: submit bildirimi gönderilemedi. form={FormId} submission={SubmissionId}", def.Id, submission.Id);
                }
            }

            // Plan 57 B1 — form-tetikli onay köprüsü (council §4.5: tek motor = WorkflowEngine).
            // TriggersWorkflowId bağlı ise submit sonrası workflow instance başlat. BEST-EFFORT:
            // fail → submit BAŞARILI kalır (submission commit'li), LogWarning + devam. IsSuccess
            // gate ZORUNLU (fail'de Data=0 → WorkflowInstanceId=0 kirliliği olurdu, council uyarı 3).
            // İdempotency: WorkflowInstanceId zaten set ise tekrar başlatma (§4.5-5 çift-instance guard).
            if (def.TriggersWorkflowId is int templateId && submission.WorkflowInstanceId is null)
            {
                try
                {
                    var wf = await workflow.StartAsync(new WorkflowStartInput(
                        FirmaId: input.FirmaId,
                        TemplateId: templateId,
                        EntityType: "FormSubmission",
                        EntityId: submission.Id,
                        StartedBy: input.SubmittedById ?? 0 /* anonim → system */), ct);

                    if (wf.IsSuccess)
                    {
                        // Geri-yazım AYRI try (silent-failure H2): instance zaten canlı — bu save fail
                        // olursa orphan instance + WorkflowInstanceId=NULL (idempotency guard delinir).
                        // Start-hatasıyla AYNI görünmemeli: LogError + orphan audit (instance id greppable).
                        submission.WorkflowInstanceId = wf.Data;
                        try { await db.SaveChangesAsync(ct); }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Forms: workflow BAŞLADI ama submission bağı yazılamadı — ORPHAN instance. submission={SubmissionId} instance={InstanceId}",
                                submission.Id, wf.Data);
                            await TryAuditAsync("form_workflow_link_orphaned", submission.Id, $"instance={wf.Data}");
                        }
                    }
                    else
                    {
                        // Silent-failure H1: onay akışı başlamadı ama submit başarılı — SADECE log yetmez
                        // (kullanıcı "onayda sanır", amir hiç görmez). Audit + form sahibine uyarı bildirimi
                        // → stale şablon bağı ilk submit'te kendini raporlar (H3'ü de kapatır).
                        logger.LogWarning("Forms: workflow tetiklenemedi. form={FormId} submission={SubmissionId} template={TemplateId} code={Code} sebep={Message}",
                            def.Id, submission.Id, templateId, wf.ErrorCode, wf.Message);
                        await TryAuditAsync("form_workflow_trigger_failed", submission.Id, $"template={templateId} code={wf.ErrorCode}");
                        await TryNotifyOwnerTriggerFailedAsync(def, submission.Id, templateId);
                    }
                }
                catch (Exception ex)
                {
                    // Beklenmedik engine hatası = warning değil ERROR (config-drift'ten ayrışsın).
                    logger.LogError(ex, "Forms: workflow tetikleme hatası. form={FormId} submission={SubmissionId} template={TemplateId}",
                        def.Id, submission.Id, templateId);
                    await TryAuditAsync("form_workflow_trigger_failed", submission.Id, $"template={templateId} code=exception");
                    await TryNotifyOwnerTriggerFailedAsync(def, submission.Id, templateId);
                }
            }

            return ServiceResult<int>.Ok(submission.Id);
        }

        // Sinyal yolları da best-effort: audit/bildirim hatası submit'i KIRMAZ (log'a düşer).
        private async Task TryAuditAsync(string eventType, int submissionId, string description)
        {
            try { await audit.LogAsync(eventType, "form_submission", submissionId.ToString(), description); }
            catch (Exception ex) { logger.LogWarning(ex, "Forms: audit yazılamadı. event={Event} submission={Id}", eventType, submissionId); }
        }

        private async Task TryNotifyOwnerTriggerFailedAsync(FormDefinition def, int submissionId, int templateId)
        {
            if (def.CreatedBy <= 0) return;
            try
            {
                await notifications.CreateAsync(
                    def.CreatedBy, "form_submission", submissionId,
                    "Forma bağlı onay akışı başlatılamadı", null,
                    $"/Forms/FormDefinition/SubmissionDetail/{submissionId}", "warning", "system");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Forms: trigger-fail bildirimi gönderilemedi. submission={Id}", submissionId);
            }
        }

        private FormSubmissionFieldValue BuildFieldValue(FormField field, string raw, bool encrypt)
        {
            var value = new FormSubmissionFieldValue { FormFieldId = field.Id, FieldKey = field.FieldKey };
            // Form şifreli ise (ihbar/DSAR): değer ValueText'e ŞİFRELİ blob olarak yazılır, tipli kolonlar
            // null kalır (aksi halde ValueNumber/Date/Bool düz metin sızdırır). Decrypt = yetki-gated (Gap 2).
            if (encrypt)
            {
                // Boş/whitespace → hiçbir kolon set etme (typed path ile tutarlı; boş değeri şifreleyip
                // IsEncrypted=true kirliliği yaratma — security-reviewer H-2).
                if (string.IsNullOrEmpty(raw)) return value;
                value.ValueText = encryption.Encrypt(raw);
                value.IsEncrypted = true;
                return value;
            }
            switch (field.FieldType)
            {
                case FormFieldType.Number:
                    // InvariantCulture ZORUNLU — survey-core "." ondalık gönderir; tr-TR host "."'ı binlik
                    // ayraç sanıp "3.5"→35 yapardı (code-reviewer locale bug; Faz 3 _elapsed'ın aynısı).
                    value.ValueNumber = decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
                    break;
                case FormFieldType.Date:
                case FormFieldType.DateTime:
                    value.ValueDate = DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
                        ? d.ToUniversalTime() : null;
                    break;
                case FormFieldType.Checkbox:
                    value.ValueBool = bool.TryParse(raw, out var b) && b;
                    break;
                default:
                    value.ValueText = raw;
                    break;
            }
            return value;
        }
    }
}
