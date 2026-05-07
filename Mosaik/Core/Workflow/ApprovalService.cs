using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Models;

namespace Mosaik.Core.Workflow
{
    // YonetIQ port — generic onay servisi. EntityType+EntityId ile herhangi
    // bir entity için onay isteği oluşturur, sıradaki onayçıyı bulur, karar uygular.
    public class ApprovalService
    {
        private readonly MosaikContext _context;

        public ApprovalService(MosaikContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<ApprovalRequest>> CreateAsync(
            string entityType,
            int entityId,
            string? subject,
            IEnumerable<(int order, int? userId, string? role)> steps,
            string createdBy)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return ServiceResult<ApprovalRequest>.Failure("EntityType zorunludur.");

            var stepList = steps.OrderBy(s => s.order).ToList();
            if (stepList.Count == 0)
                return ServiceResult<ApprovalRequest>.Failure("En az bir onay adımı gerekli.");

            var request = new ApprovalRequest
            {
                EntityType = entityType,
                EntityId = entityId,
                Subject = subject,
                Status = ApprovalStatus.Pending,
                CreatedBy = createdBy
            };

            foreach (var (order, userId, role) in stepList)
            {
                request.Steps.Add(new ApprovalStep
                {
                    StepOrder = order,
                    ApproverUserId = userId,
                    ApproverRole = role,
                    Status = ApprovalStatus.Pending,
                    CreatedBy = createdBy
                });
            }

            _context.ApprovalRequests.Add(request);
            await _context.SaveChangesAsync();

            return ServiceResult<ApprovalRequest>.Ok(request, "Onay istegi olusturuldu.");
        }

        // MIN(StepOrder) içinde Pending olan adım — sıradaki onayçı.
        public async Task<ApprovalStep?> GetCurrentStepAsync(int requestId)
        {
            return await _context.ApprovalSteps
                .Where(s => s.RequestId == requestId && s.Status == ApprovalStatus.Pending)
                .OrderBy(s => s.StepOrder)
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult> DecideAsync(
            int stepId,
            ApprovalStatus decision,
            int decidingUserId,
            string? comment = null)
        {
            if (decision != ApprovalStatus.Approved && decision != ApprovalStatus.Rejected)
                return ServiceResult.Failure("Karar yalnizca Approved veya Rejected olabilir.");

            var step = await _context.ApprovalSteps.FindAsync(stepId);
            if (step == null) return ServiceResult.Failure("Adim bulunamadi.");
            if (step.Status != ApprovalStatus.Pending)
                return ServiceResult.Failure("Adim zaten karara baglandi.");

            step.Status = decision;
            step.DecidedAt = DateTime.UtcNow;
            step.Comment = comment;
            step.UpdatedBy = decidingUserId.ToString();
            step.UpdatedAt = DateTime.UtcNow;

            // Reddedildiyse istek baştan reddedilir, sonraki adımlara gerek yok.
            if (decision == ApprovalStatus.Rejected)
            {
                var request = await _context.ApprovalRequests.FindAsync(step.RequestId);
                if (request != null)
                {
                    request.Status = ApprovalStatus.Rejected;
                    request.CompletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Approved — son adım mıydı kontrol et.
                var pendingCount = await _context.ApprovalSteps
                    .CountAsync(s => s.RequestId == step.RequestId
                                  && s.Status == ApprovalStatus.Pending
                                  && s.Id != stepId);
                if (pendingCount == 0)
                {
                    var request = await _context.ApprovalRequests.FindAsync(step.RequestId);
                    if (request != null)
                    {
                        request.Status = ApprovalStatus.Approved;
                        request.CompletedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Karar kaydedildi.");
        }

        // Belirli bir kullanıcının bekleyen onaylarını döner (sıradaki adım).
        public async Task<List<ApprovalStep>> GetPendingForUserAsync(int userId, IEnumerable<string> userRoles)
        {
            var roleSet = userRoles.ToHashSet();

            // Pending olan ve aynı request'in sıradaki (MIN StepOrder) adımı olan.
            var pendingSteps = await _context.ApprovalSteps
                .Where(s => s.Status == ApprovalStatus.Pending
                         && (s.ApproverUserId == userId
                             || (s.ApproverRole != null && roleSet.Contains(s.ApproverRole))))
                .ToListAsync();

            // Sadece kendi request'inin MIN(StepOrder) Pending olanları döndür.
            var result = new List<ApprovalStep>();
            foreach (var step in pendingSteps)
            {
                var minPending = await _context.ApprovalSteps
                    .Where(s => s.RequestId == step.RequestId && s.Status == ApprovalStatus.Pending)
                    .MinAsync(s => (int?)s.StepOrder);
                if (minPending == step.StepOrder)
                    result.Add(step);
            }
            return result;
        }
    }
}
