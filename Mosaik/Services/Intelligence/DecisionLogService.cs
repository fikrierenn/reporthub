using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Intelligence;
using Mosaik.Models;
using Mosaik.Models.Intelligence;

namespace Mosaik.Services.Intelligence
{
    // Plan 38 — Decision Memory servisi.
    // AuditLog ne olduğunu der, DecisionLog neden + alternatifler + beklenen sonuç der.
    public class DecisionLogService : IDecisionLogService
    {
        private readonly MosaikContext _context;
        private readonly ILogger<DecisionLogService> _logger;

        public DecisionLogService(MosaikContext context, ILogger<DecisionLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<int>> LogAsync(DecisionLogEntry entry, CancellationToken ct = default)
        {
            if (entry.FirmaId <= 0 || entry.MadeBy <= 0)
                return ServiceResult<int>.Failure("FirmaId ve MadeBy pozitif olmalı.", "INVALID_ID");
            if (string.IsNullOrWhiteSpace(entry.Title))
                return ServiceResult<int>.Failure("Karar başlığı boş olamaz.", "INVALID_TITLE");
            if (entry.RelatedEntityType is not null && !EntityType.IsValid(entry.RelatedEntityType))
                return ServiceResult<int>.Failure($"Geçersiz RelatedEntityType: {entry.RelatedEntityType}", "INVALID_ENTITY_TYPE");

            var log = new DecisionLog
            {
                FirmaId = entry.FirmaId,
                Title = entry.Title,
                Rationale = entry.Rationale,
                MadeBy = entry.MadeBy,
                MadeAt = entry.MadeAt ?? DateTime.UtcNow,
                AlternativesJson = entry.AlternativesJson,
                ExpectedOutcome = entry.ExpectedOutcome,
                KpiImpactJson = entry.KpiImpactJson,
                RelatedEntityType = entry.RelatedEntityType,
                RelatedEntityId = entry.RelatedEntityId,
                RelatedAuditId = entry.RelatedAuditId,
                Status = "Active"
            };
            _context.DecisionLogs.Add(log);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(log.Id, "Karar kayıt edildi.");
        }

        public async Task<IReadOnlyList<DecisionLogDto>> GetByEntityAsync(
            string relatedEntityType, int relatedEntityId, CancellationToken ct = default)
        {
            return await _context.DecisionLogs.AsNoTracking()
                .Where(d => d.RelatedEntityType == relatedEntityType && d.RelatedEntityId == relatedEntityId)
                .OrderByDescending(d => d.MadeAt)
                .Select(d => new DecisionLogDto(
                    d.Id, d.FirmaId, d.Title, d.Rationale, d.MadeBy, d.MadeAt,
                    d.AlternativesJson, d.ExpectedOutcome, d.ActualOutcome, d.KpiImpactJson,
                    d.RelatedEntityType, d.RelatedEntityId, d.RelatedAuditId, d.Status))
                .ToListAsync(ct);
        }
    }
}
