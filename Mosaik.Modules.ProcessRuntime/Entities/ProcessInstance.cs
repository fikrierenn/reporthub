using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Modules.ProcessRuntime.Entities
{
    // Plan 42 REV 3 — ProcessInstance = İNCE VAKA KONTEYNERİ (council §4.5: state machine DEĞİL).
    // Yürütme otoritesi = WorkflowInstance (soft-ref WorkflowInstanceId); Status buradaki kolon
    // yalnızca KABA projeksiyondur (Açık/Tamamlandı/İptal — tek-yön güncellenir, geri yazmaz).
    // Adım/assignee/step-state ASLA burada yaşamaz (drift yasak — REV 3 #3/#4/#6).
    public class ProcessInstance
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // İnsan-dostu vaka kodu: "PI-20260705-0001" (ProcessInstanceCode.Generate).
        [Required, MaxLength(40)]
        public string InstanceCode { get; set; } = string.Empty;

        [Required]
        public int FirmaId { get; set; }

        // Plan 40 KVKK Process tanımına SOFT ref (KvkkProcesses.Id) — modül compile bağı YASAK
        // (ADR-002; KVKK modülü opsiyonel). NULL = tanımsız/serbest vaka.
        public int? ProcessId { get; set; }

        // Kaba durum PROJEKSİYONU: 0 Açık · 1 Tamamlandı · 2 İptal. Otorite WorkflowInstance
        // status'u; ProcessStatusProjector tek-yön günceller (REV 3 #3/#6).
        public byte Status { get; set; }

        public byte Priority { get; set; } = 1;   // 0 Düşük 1 Normal 2 Yüksek 3 Acil

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public int? InitiatedById { get; set; }   // NULL = anonim/cron

        [MaxLength(200)]
        public string? InitiatorEmail { get; set; }

        // Ana yürütücü workflow (soft-ref WorkflowInstances.Id). NULL = workflow'suz kayıt-vakası.
        public int? WorkflowInstanceId { get; set; }

        public DateTime? DueAt { get; set; }      // SLA hatırlatma projeksiyonu (tetikleyici DEĞİL — REV 3 #5)

        [BindNever]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(40)]
        public string? ResultCode { get; set; }   // "Approved" / "Rejected" / "Closed"

        [Required, MaxLength(40)]
        public string SourceTrigger { get; set; } = "manual";  // manual | public | cron | event

        public int? SourceTriggerRefId { get; set; }           // parent instance / token / job ref

        public List<ProcessInstanceAspect> Aspects { get; set; } = new();
    }

    public static class ProcessInstanceStatus
    {
        public const byte Open = 0;
        public const byte Completed = 1;
        public const byte Cancelled = 2;
    }
}
