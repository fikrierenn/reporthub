using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.ViewModels.Workflow
{
    // Plan 36 — Admin designer için template oluştur/düzenle form modeli.
    public class WorkflowTemplateViewModel
    {
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        public int FirmaId { get; set; }

        [Required(ErrorMessage = "Şablon adı gerekli.")]
        [MaxLength(200)]
        [Display(Name = "Şablon Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bağlandığı modül seçimi gerekli.")]
        [MaxLength(50)]
        [Display(Name = "Bağlı Modül")]
        public string EntityType { get; set; } = string.Empty;

        // sequential-workflow-designer'ın .getDefinition() çıktısı (JSON string).
        // Form post'unda hidden input olarak gelir.
        [Required(ErrorMessage = "Şablon tanımı boş olamaz. Designer'dan en az 1 adım ekleyin.")]
        public string DefinitionJson { get; set; } = string.Empty;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class WorkflowAdminListViewModel
    {
        public IReadOnlyList<TemplateListItem> Templates { get; set; } = Array.Empty<TemplateListItem>();
        public IReadOnlyList<InstanceListItem> RecentInstances { get; set; } = Array.Empty<InstanceListItem>();
    }

    public class TemplateListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int InstanceCount { get; set; }
    }

    public class InstanceListItem
    {
        public int Id { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string? CurrentStepLabel { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    // Kullanıcının kendi inbox'ı — bana atanan aktif step'ler.
    public class WorkflowInboxViewModel
    {
        public IReadOnlyList<InboxItem> Items { get; set; } = Array.Empty<InboxItem>();
    }

    public class InboxItem
    {
        public int InstanceId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string StepLabel { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
