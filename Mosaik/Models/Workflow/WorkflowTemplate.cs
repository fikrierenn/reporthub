using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models.Workflow
{
    // Plan 36 — sequential-workflow-designer ile çizilen template.
    [Table("WorkflowTemplates")]
    public class WorkflowTemplate
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int FirmaId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public string DefinitionJson { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public int? CreatedBy { get; set; }

        [BindNever]
        public DateTime? UpdatedAt { get; set; }

        [BindNever]
        public int? UpdatedBy { get; set; }
    }
}
