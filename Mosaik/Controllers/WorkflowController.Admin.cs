using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models.Workflow;
using Mosaik.Services;
using Mosaik.ViewModels.Workflow;

namespace Mosaik.Controllers
{
    // Plan 36 W-06 / W-07 — Admin workflow template CRUD + canlı instance görünüm.
    // Route prefix: /Workflow/Admin. Tüm action'lar admin rolü.
    [Authorize(Roles = "admin")]
    public partial class WorkflowController
    {
        // GET /Workflow/Admin
        [HttpGet("Workflow/Admin")]
        public async Task<IActionResult> AdminIndex(CancellationToken ct)
        {
            var templates = await _db.WorkflowTemplates.AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TemplateListItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    EntityType = t.EntityType,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    InstanceCount = _db.WorkflowInstances.Count(i => i.TemplateId == t.Id)
                })
                .ToListAsync(ct);

            var instances = await _db.WorkflowInstances.AsNoTracking()
                .Include(i => i.Template)
                .OrderByDescending(i => i.StartedAt)
                .Take(20)
                .Select(i => new InstanceListItem
                {
                    Id = i.Id,
                    TemplateName = i.Template!.Name,
                    EntityType = i.EntityType,
                    EntityId = i.EntityId,
                    CurrentStepLabel = i.CurrentStepId,
                    StatusLabel = i.Status.ToString(),
                    StartedAt = i.StartedAt,
                    CompletedAt = i.CompletedAt
                })
                .ToListAsync(ct);

            return View("AdminIndex", new WorkflowAdminListViewModel
            {
                Templates = templates,
                RecentInstances = instances
            });
        }

        // GET /Workflow/Admin/Create
        [HttpGet("Workflow/Admin/Create")]
        public IActionResult AdminCreate()
        {
            return View("AdminTemplateForm", new WorkflowTemplateViewModel
            {
                IsActive = true,
                DefinitionJson = "{\"steps\":[]}"
            });
        }

        // POST /Workflow/Admin/Create
        [HttpPost("Workflow/Admin/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreate(WorkflowTemplateViewModel input, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("AdminTemplateForm", input);

            if (!TryGetUserId(out var userId)) return Forbid();
            var firmaId = ResolveFirmaId();

            var template = new WorkflowTemplate
            {
                FirmaId = firmaId,
                Name = input.Name.Trim(),
                EntityType = input.EntityType.Trim(),
                DefinitionJson = input.DefinitionJson,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            _db.WorkflowTemplates.Add(template);
            await _db.SaveChangesAsync(ct);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "workflow_template_create",
                TargetType = "workflow_template",
                TargetKey = template.Id.ToString(),
                Description = $"Template oluşturuldu: {template.Name}",
                IsSuccess = true
            });

            TempData["Message"] = "Şablon oluşturuldu.";
            return RedirectToAction(nameof(AdminIndex));
        }

        // GET /Workflow/Admin/Edit/{id}
        [HttpGet("Workflow/Admin/Edit/{id:int}")]
        public async Task<IActionResult> AdminEdit(int id, CancellationToken ct)
        {
            var template = await _db.WorkflowTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (template is null) return NotFound();

            return View("AdminTemplateForm", new WorkflowTemplateViewModel
            {
                Id = template.Id,
                FirmaId = template.FirmaId,
                Name = template.Name,
                EntityType = template.EntityType,
                DefinitionJson = template.DefinitionJson,
                IsActive = template.IsActive
            });
        }

        // POST /Workflow/Admin/Edit/{id}
        [HttpPost("Workflow/Admin/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEdit(int id, WorkflowTemplateViewModel input, CancellationToken ct)
        {
            var template = await _db.WorkflowTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (template is null) return NotFound();

            if (!ModelState.IsValid)
            {
                input.Id = id;
                input.FirmaId = template.FirmaId;
                return View("AdminTemplateForm", input);
            }

            if (!TryGetUserId(out var userId)) return Forbid();

            template.Name = input.Name.Trim();
            template.EntityType = input.EntityType.Trim();
            template.DefinitionJson = input.DefinitionJson;
            template.IsActive = input.IsActive;
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = userId;
            await _db.SaveChangesAsync(ct);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "workflow_template_update",
                TargetType = "workflow_template",
                TargetKey = template.Id.ToString(),
                Description = $"Template güncellendi: {template.Name}",
                IsSuccess = true
            });

            TempData["Message"] = "Şablon güncellendi.";
            return RedirectToAction(nameof(AdminIndex));
        }

        // POST /Workflow/Admin/Toggle/{id}
        [HttpPost("Workflow/Admin/Toggle/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminToggle(int id, CancellationToken ct)
        {
            var template = await _db.WorkflowTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (template is null) return NotFound();

            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.UtcNow;
            if (TryGetUserId(out var userId)) template.UpdatedBy = userId;
            await _db.SaveChangesAsync(ct);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = template.IsActive ? "workflow_template_activate" : "workflow_template_deactivate",
                TargetType = "workflow_template",
                TargetKey = template.Id.ToString(),
                Description = $"Template {(template.IsActive ? "aktif" : "pasif")}: {template.Name}",
                IsSuccess = true
            });

            TempData["Message"] = template.IsActive ? "Şablon aktifleştirildi." : "Şablon pasifleştirildi.";
            return RedirectToAction(nameof(AdminIndex));
        }

        // FirmaId çözümü — kullanıcının ilk firması (multi-firma kullanıcıları için
        // Plan 14 Faz B sonrası context picker gerek; şimdilik ilk firma).
        private int ResolveFirmaId()
        {
            var firmaClaim = User.FindFirst("FirmaId")?.Value;
            if (int.TryParse(firmaClaim, out var fid) && fid > 0) return fid;
            return 1; // fallback — tek firmalı kurulum
        }
    }

    // Plan 36 W-14/W-15 — generic workflow trigger (Contract/Obligation/Circular/Document/...).
    // Auth: herhangi bir authenticated user (modül-spesifik permission yok şimdilik).
    public partial class WorkflowController
    {
        // GET /Workflow/Trigger?entityType=Contract&entityId=42
        [HttpGet("Workflow/Trigger")]
        [Authorize]
        public async Task<IActionResult> Trigger(string entityType, int entityId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0) return BadRequest();

            var templates = await _db.WorkflowTemplates.AsNoTracking()
                .Where(t => t.IsActive && t.EntityType == entityType)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync(ct);

            ViewBag.EntityType = entityType;
            ViewBag.EntityId = entityId;
            ViewBag.Templates = templates;
            return View("Trigger");
        }

        // POST /Workflow/Trigger
        [HttpPost("Workflow/Trigger")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Trigger(TriggerViewModel input, CancellationToken ct)
        {
            if (input is null || string.IsNullOrWhiteSpace(input.EntityType)
                || input.EntityId <= 0 || input.TemplateId <= 0)
                return BadRequest();
            if (!TryGetUserId(out var userId)) return Forbid();

            var template = await _db.WorkflowTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == input.TemplateId && t.IsActive, ct);
            if (template is null || template.EntityType != input.EntityType)
            {
                TempData["Message"] = "Seçilen şablon bu modüle uygun değil.";
                return RedirectToAction(nameof(Trigger), new { entityType = input.EntityType, entityId = input.EntityId });
            }

            var firmaId = template.FirmaId; // template hangi firmaya aitse instance da o firmaya
            var start = new Core.Workflow.WorkflowStartInput(
                FirmaId: firmaId,
                TemplateId: input.TemplateId,
                EntityType: input.EntityType,
                EntityId: input.EntityId,
                StartedBy: userId,
                PayloadJson: null);

            var result = await _engine.StartAsync(start, ct);
            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "workflow_instance_start",
                TargetType = "workflow_instance",
                TargetKey = result.Data.ToString(),
                Description = $"{input.EntityType} #{input.EntityId} → şablon {template.Name}",
                IsSuccess = result.IsSuccess
            });

            if (!result.IsSuccess)
            {
                TempData["Message"] = result.Message;
                return RedirectToAction(nameof(Trigger), new { entityType = input.EntityType, entityId = input.EntityId });
            }

            TempData["Message"] = "Onay akışı başlatıldı.";
            return RedirectToAction(nameof(Instance), new { id = result.Data });
        }

        public class TriggerViewModel
        {
            public string EntityType { get; set; } = string.Empty;
            public int EntityId { get; set; }
            public int TemplateId { get; set; }
        }
    }
}
