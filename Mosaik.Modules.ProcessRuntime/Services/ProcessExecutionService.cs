using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Core.Workflow;
using Mosaik.Modules.ProcessRuntime.Entities;

namespace Mosaik.Modules.ProcessRuntime.Services
{
    public sealed record ProcessStartInput(
        int FirmaId, string Title, int StartedBy,
        int? ProcessId = null, int? WorkflowTemplateId = null,
        byte Priority = 1, DateTime? DueAt = null,
        string SourceTrigger = "manual", int? SourceTriggerRefId = null);

    // Plan 42 REV 3 — vaka yaşam döngüsü. İLERLET YOK (council #1): adım kararları
    // WorkflowController.Respond → IWorkflowService.AdvanceAsync'te yaşar. Burada sadece:
    // StartAsync (vaka + opsiyonel workflow başlat + Workflow aspect), CancelAsync (workflow
    // Cancel delege + kaba-status İptal), RefreshStatusAsync (tek-yön projeksiyon: bağlı
    // workflow bitti/iptal ise vakayı kapat — ProcessInstance'tan workflow'a ASLA yazılmaz).
    public class ProcessExecutionService(DbContext db, IWorkflowService workflow, ILogger<ProcessExecutionService> logger)
    {
        private DbSet<ProcessInstance> Instances => db.Set<ProcessInstance>();

        public async Task<ServiceResult<int>> StartAsync(ProcessStartInput input, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                return ServiceResult<int>.Failure("Vaka başlığı zorunlu.");

            var instance = new ProcessInstance
            {
                FirmaId = input.FirmaId,
                ProcessId = input.ProcessId,
                Title = input.Title.Trim(),
                Priority = input.Priority,
                InitiatedById = input.StartedBy > 0 ? input.StartedBy : null,
                DueAt = input.DueAt,
                SourceTrigger = input.SourceTrigger,
                SourceTriggerRefId = input.SourceTriggerRefId,
                Status = ProcessInstanceStatus.Open,
                // Security M-1: sabit "PENDING" sentinel'i UQ_PI_InstanceCode'da eşzamanlı
                // ikinci create'i 500'letirdi + crash'te slotu kalıcı işgal ederdi → benzersiz geçici.
                InstanceCode = $"PENDING-{Guid.NewGuid():N}"
            };
            Instances.Add(instance);
            await db.SaveChangesAsync(ct);

            // İnsan-dostu kod Id'den üretilir (çakışmasız): PI-yyyyMMdd-Id.
            instance.InstanceCode = ProcessInstanceCode.Generate(instance.StartedAt, instance.Id);

            // Opsiyonel yürütücü workflow — TEK motor (council): şablon verildiyse başlat.
            if (input.WorkflowTemplateId is int templateId)
            {
                var wf = await workflow.StartAsync(new WorkflowStartInput(
                    FirmaId: input.FirmaId,
                    TemplateId: templateId,
                    EntityType: "ProcessInstance",
                    EntityId: instance.Id,
                    StartedBy: input.StartedBy), ct);

                if (wf.IsSuccess)
                {
                    instance.WorkflowInstanceId = wf.Data;
                    instance.Aspects.Add(new ProcessInstanceAspect
                    {
                        AspectType = ProcessAspectType.Workflow,
                        AspectId = wf.Data,
                        RelationLabel = "yürütücü akış",
                        AddedBy = input.StartedBy > 0 ? input.StartedBy : null
                    });
                }
                else
                {
                    // Vaka workflow'suz açılmaz — yarım konteyner bırakma (fail-closed).
                    Instances.Remove(instance);
                    await db.SaveChangesAsync(ct);
                    return ServiceResult<int>.Failure($"Onay akışı başlatılamadı: {wf.Message}");
                }
            }

            await db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(instance.Id);
        }

        public async Task<ServiceResult<bool>> CancelAsync(int instanceId, int firmaId, int actorId, string? reason, CancellationToken ct = default)
        {
            var instance = await Instances.FirstOrDefaultAsync(i => i.Id == instanceId && i.FirmaId == firmaId, ct);
            if (instance is null)
                return ServiceResult<bool>.Failure("Vaka bulunamadı.");
            if (instance.Status != ProcessInstanceStatus.Open)
                return ServiceResult<bool>.Failure("Vaka zaten kapalı.");

            // Yürütücü workflow'u motor iptal eder (delege — kendi state machine YOK).
            if (instance.WorkflowInstanceId is int wfId)
            {
                // Security H-2 (defense-in-depth): IWorkflowService.CancelAsync firma bilmez —
                // delege ÖNCESİ bağlı workflow'un firması vaka firmasıyla eşleşmeli. (Engine-side
                // firma-scope Core interface değişikliği — ayrı borç, mosaik-security notu.)
                var wfFirma = await db.Database
                    .SqlQueryRaw<int>("SELECT FirmaId AS Value FROM dbo.WorkflowInstances WHERE Id = {0}", wfId)
                    .ToListAsync(ct);
                if (wfFirma.Count > 0 && wfFirma[0] != firmaId)
                {
                    logger.LogError("ProcessInstance {Id}: bağlı workflow {WfId} farklı firmaya ait ({WfFirma} != {Firma}) — iptal reddedildi.",
                        instanceId, wfId, wfFirma[0], firmaId);
                    return ServiceResult<bool>.Failure("Vaka iptal edilemedi.");
                }

                var r = await workflow.CancelAsync(wfId, actorId, reason, ct);
                if (!r.IsSuccess)
                {
                    // Security H-1 (fail-closed): otorite (workflow) iptali reddettiyse vaka SAHTE
                    // kapatılmaz — projeksiyon otoriteyle çelişemez. Tek istisna: workflow kaydı
                    // hiç yoksa (orphan) konteyner kapatılabilir.
                    if (wfFirma.Count == 0)
                    {
                        logger.LogWarning("ProcessInstance {Id}: workflow {WfId} bulunamadı (orphan) — konteyner kapatılıyor.", instanceId, wfId);
                    }
                    else
                    {
                        logger.LogWarning("ProcessInstance {Id}: workflow {WfId} iptal edilemedi ({Msg}) — vaka AÇIK kalıyor.",
                            instanceId, wfId, r.Message);
                        return ServiceResult<bool>.Failure("Yürütücü onay akışı iptal edilemediği için vaka kapatılamadı.");
                    }
                }
            }

            instance.Status = ProcessInstanceStatus.Cancelled;
            instance.CompletedAt = DateTime.UtcNow;
            instance.ResultCode = "Cancelled";
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        // Tek-yön projeksiyon (REV 3 #3/#6): bağlı workflow tamamlandı/iptal ise kaba-status'u
        // eşitle. Okuma yollarından (Details/Index) idempotent çağrılır; workflow'a geri yazmaz.
        // WorkflowInstanceStatus: 0 Active 1 Completed 2 Cancelled 3 Rejected (Core enum değerleri
        // sorguda raw okunur — modül ana-proje enum'una compile bağlanmaz).
        // Firma sınırı (security M-2): wfId yalnız firma-guard'lı vaka satırından gelir — okuma
        // read-only + Detail zaten firma doğruladı; enforced boundary = vaka guard'ı.
        public async Task RefreshStatusAsync(ProcessInstance instance, CancellationToken ct = default)
        {
            if (instance.Status != ProcessInstanceStatus.Open || instance.WorkflowInstanceId is not int wfId)
                return;

            var wfStatus = await db.Database
                .SqlQueryRaw<int>("SELECT Status AS Value FROM dbo.WorkflowInstances WHERE Id = {0}", wfId)
                .ToListAsync(ct);
            if (wfStatus.Count == 0) return;

            var s = wfStatus[0];
            if (s == 0) return; // hâlâ aktif

            instance.Status = s == 2 ? ProcessInstanceStatus.Cancelled : ProcessInstanceStatus.Completed;
            instance.ResultCode = s switch { 1 => "Approved", 3 => "Rejected", 2 => "Cancelled", _ => "Closed" };
            instance.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        public async Task<ServiceResult<int>> AddAspectAsync(int instanceId, int firmaId, byte aspectType, int aspectId, string? label, int? addedBy, CancellationToken ct = default)
        {
            var owned = await Instances.AsNoTracking().AnyAsync(i => i.Id == instanceId && i.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult<int>.Failure("Vaka bulunamadı.");

            var aspect = new ProcessInstanceAspect
            {
                ProcessInstanceId = instanceId,
                AspectType = aspectType,
                AspectId = aspectId,
                RelationLabel = label,
                AddedBy = addedBy
            };
            db.Set<ProcessInstanceAspect>().Add(aspect);
            await db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(aspect.Id);
        }
    }

    // Saf üretici — test edilebilir. "PI-20260705-0042" (Id çakışmasız benzersizlik garantisi).
    public static class ProcessInstanceCode
    {
        public static string Generate(DateTime startedAtUtc, int id)
            => $"PI-{startedAtUtc:yyyyMMdd}-{id:D4}";
    }
}
