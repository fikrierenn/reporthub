using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.ProcessRuntime.Entities;

namespace Mosaik.Modules.ProcessRuntime.Services
{
    // Plan 42 REV 3 — READ katmanı: liste + vaka timeline'ı. Timeline = ProcessInstanceAspects
    // + bağlı WorkflowInstanceLogs BİRLEŞİK görünümü (council #2: transitions tablosu yok,
    // WorkflowInstanceLogs tek log otoritesi — buradan raw okunur, modül compile bağı yok).
    public class ProcessInstanceQueryService(DbContext db)
    {
        public sealed record ListRow(int Id, string InstanceCode, string Title, byte Status, byte Priority,
            DateTime StartedAt, DateTime? DueAt, string SourceTrigger, int? WorkflowInstanceId);

        public sealed record TimelineEvent(DateTime At, string Kind, string Label, string? Detail);

        public sealed record Detail(ProcessInstance Instance, IReadOnlyList<ProcessInstanceAspect> Aspects,
            IReadOnlyList<TimelineEvent> Timeline);

        public async Task<IReadOnlyList<ListRow>> ListAsync(int firmaId, int? initiatedById = null, byte? status = null, CancellationToken ct = default)
        {
            var q = db.Set<ProcessInstance>().AsNoTracking()
                .Where(i => i.FirmaId == firmaId);
            if (initiatedById is int uid) q = q.Where(i => i.InitiatedById == uid);
            if (status is byte s) q = q.Where(i => i.Status == s);

            return await q.OrderByDescending(i => i.StartedAt)
                .Take(200)
                .Select(i => new ListRow(i.Id, i.InstanceCode, i.Title, i.Status, i.Priority,
                    i.StartedAt, i.DueAt, i.SourceTrigger, i.WorkflowInstanceId))
                .ToListAsync(ct);
        }

        public async Task<Detail?> DetailAsync(int id, int firmaId, CancellationToken ct = default)
        {
            var instance = await db.Set<ProcessInstance>()
                .Include(i => i.Aspects)
                .FirstOrDefaultAsync(i => i.Id == id && i.FirmaId == firmaId, ct);
            if (instance is null) return null;

            var timeline = new List<TimelineEvent>
            {
                new(instance.StartedAt, "instance", "Vaka açıldı", instance.SourceTrigger)
            };

            foreach (var a in instance.Aspects.OrderBy(a => a.AddedAt))
                timeline.Add(new TimelineEvent(a.AddedAt, "aspect",
                    AspectLabel(a.AspectType), a.RelationLabel));

            // Bağlı workflow event stream'i (tek otorite) — raw SQL: modül WorkflowInstanceLog
            // CLR tipine compile bağlanamaz (ana proje entity'si).
            if (instance.WorkflowInstanceId is int wfId)
            {
                var logs = await db.Database.SqlQueryRaw<WorkflowLogRow>(
                        "SELECT StepId, EventType, OccurredAt FROM dbo.WorkflowInstanceLogs WHERE InstanceId = {0} ORDER BY Id", wfId)
                    .ToListAsync(ct);
                foreach (var l in logs)
                    timeline.Add(new TimelineEvent(l.OccurredAt, "workflow",
                        EventLabel(l.EventType), l.StepId));
            }

            if (instance.CompletedAt is DateTime done)
                timeline.Add(new TimelineEvent(done, "instance",
                    instance.Status == ProcessInstanceStatus.Cancelled ? "Vaka iptal edildi" : "Vaka kapandı",
                    instance.ResultCode));

            return new Detail(instance, instance.Aspects.OrderBy(a => a.AddedAt).ToList(),
                timeline.OrderBy(t => t.At).ToList());
        }

        private sealed record WorkflowLogRow(string? StepId, string EventType, DateTime OccurredAt);

        public static string AspectLabel(byte t) => t switch
        {
            ProcessAspectType.Form => "Form yanıtı bağlandı",
            ProcessAspectType.Workflow => "Onay akışı bağlandı",
            ProcessAspectType.Document => "Belge bağlandı",
            ProcessAspectType.Decision => "Karar kaydı",
            ProcessAspectType.Audit => "Denetim kaydı",
            ProcessAspectType.KvkkContext => "KVKK bağlamı",
            ProcessAspectType.Comment => "Yorum",
            ProcessAspectType.SubInstance => "Alt vaka",
            _ => "Bağlantı"
        };

        public static string EventLabel(string eventType) => eventType switch
        {
            "InstanceStarted" => "Akış başladı",
            "StepEntered" => "Adıma girildi",
            "StepCompleted" => "Adım onaylandı",
            "StepRejected" => "Adım reddedildi",
            "InstanceCompleted" => "Akış tamamlandı",
            "InstanceCancelled" => "Akış iptal edildi",
            "ManagerResolved" => "Amir çözüldü",
            "ManagerResolutionFailed" => "Amir çözülemedi (rol fallback)",
            "DelayScheduled" => "Bekleme planlandı",
            _ => eventType
        };

        public static string StatusLabel(byte s) => s switch
        {
            ProcessInstanceStatus.Open => "Açık",
            ProcessInstanceStatus.Completed => "Tamamlandı",
            ProcessInstanceStatus.Cancelled => "İptal",
            _ => s.ToString()
        };

        public static string StatusClass(byte s) => s switch
        {
            ProcessInstanceStatus.Open => "warn",
            ProcessInstanceStatus.Completed => "ok",
            _ => "err"
        };
    }
}
