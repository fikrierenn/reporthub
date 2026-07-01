using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 §4.3 (M6 Faz 5) — 8 pattern detector (saf-C#, Presidio ertelendi — advisor kararı).
    // Faz 4 precedent: junction/entity strongly-typed okuma, EntityRelations'a bağımlılık yok.
    public record KvkkFindingCandidate(string PatternCode, byte Severity, int? ProcessId, string Description);

    public class KvkkIntegrityChecker(DbContext db)
    {
        private const byte SeverityIdari = 0;
        private const byte SeverityIdariYuksek = 1;
        private const byte SeverityKritik = 2;

        public async Task<List<KvkkFindingCandidate>> ScanAsync(int firmaId, CancellationToken ct = default)
        {
            var processes = await db.Set<KvkkProcess>().AsNoTracking()
                .Where(p => p.FirmaId == firmaId && p.IsActive)
                .Include(p => p.LegalBasis)
                .Include(p => p.RetentionRule)
                .Include(p => p.DataLinks).ThenInclude(l => l.DataElement)
                .Include(p => p.CrossBorderTransfers)
                .ToListAsync(ct);

            var findings = new List<KvkkFindingCandidate>();

            findings.AddRange(DetectCopyPastePurpose(processes));
            findings.AddRange(DetectConsentLegalConflict(processes));
            findings.AddRange(DetectCctvOverLimit(processes));
            findings.AddRange(DetectCandidateCvOverLimit(processes));
            findings.AddRange(DetectHiddenCrossBorderTransfer(processes));
            findings.AddRange(DetectMissingLegalProcesses(processes));
            findings.AddRange(await DetectSopDataElementGapAsync(processes, firmaId, ct));
            // Pattern 4 (disclosure-mismatch) tespit edilmiyor — DisclosureNotice entity Faz 6'da ertelendi.

            return findings;
        }

        // Pattern 8 (Faz 3) — SOP'ta taranan veri öğesi, bağlı sürecin ProcessDataLink'inde yok.
        // SopDocument entity'sine hiç dokunmaz (junction + scan sonucu okunur — ADR-002).
        private async Task<IEnumerable<KvkkFindingCandidate>> DetectSopDataElementGapAsync(
            List<KvkkProcess> processes, int firmaId, CancellationToken ct)
        {
            var sopLinks = await db.Set<SopProcessLink>().AsNoTracking()
                .Where(l => l.FirmaId == firmaId).ToListAsync(ct);
            if (sopLinks.Count == 0)
                return [];

            var sopIds = sopLinks.Select(l => l.SopDocumentId).Distinct().ToList();
            var scannedByDoc = (await db.Set<SopScannedElement>().AsNoTracking()
                    .Where(s => sopIds.Contains(s.SopDocumentId)).ToListAsync(ct))
                .GroupBy(s => s.SopDocumentId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.DataElementId).ToHashSet());

            var processById = processes.ToDictionary(p => p.Id);
            var result = new List<KvkkFindingCandidate>();
            foreach (var link in sopLinks)
            {
                if (!processById.TryGetValue(link.ProcessId, out var p)) continue;
                if (!scannedByDoc.TryGetValue(link.SopDocumentId, out var scannedIds)) continue;
                var linkedIds = p.DataLinks.Select(l => l.DataElementId).ToHashSet();
                var missing = scannedIds.Except(linkedIds).Count();
                if (missing == 0) continue;
                result.Add(new KvkkFindingCandidate(
                    KvkkIntegrityPatterns.SopDataElementGap, SeverityIdari, p.Id,
                    $"\"{link.SopTitle}\" SOP'unda {missing} veri öğesi geçiyor ama \"{p.Name}\" sürecinde tanımlı değil."));
            }
            return result;
        }

        private static IEnumerable<KvkkFindingCandidate> DetectCopyPastePurpose(List<KvkkProcess> processes)
        {
            var groups = new List<List<KvkkProcess>>();
            foreach (var p in processes.Where(p => !string.IsNullOrWhiteSpace(p.Purpose)))
            {
                var group = groups.FirstOrDefault(g => KvkkIntegrityRules.IsCopyPastePurpose(g[0].Purpose, p.Purpose));
                if (group != null) group.Add(p);
                else groups.Add([p]);
            }

            foreach (var group in groups.Where(g => g.Count >= 3))
                foreach (var p in group)
                    yield return new KvkkFindingCandidate(
                        KvkkIntegrityPatterns.CopyPastePurpose, SeverityIdari, p.Id,
                        $"\"{p.Name}\" süreci {group.Count - 1} başka süreçle aynı işleme amacını kullanıyor (kopyala-yapıştır şüphesi).");
        }

        private static IEnumerable<KvkkFindingCandidate> DetectConsentLegalConflict(List<KvkkProcess> processes)
        {
            foreach (var p in processes)
                if (KvkkIntegrityRules.IsConsentLegalConflict(p.LegalBasis?.Code ?? string.Empty, p.Department, p.Name))
                    yield return new KvkkFindingCandidate(
                        KvkkIntegrityPatterns.ConsentLegalConflict, SeverityIdariYuksek, p.Id,
                        $"\"{p.Name}\" süreci hukuki yükümlülük gerektirirken hukuki sebep olarak açık rıza (m.5/1) seçilmiş.");
        }

        private static IEnumerable<KvkkFindingCandidate> DetectCctvOverLimit(List<KvkkProcess> processes)
        {
            foreach (var p in processes)
            {
                if (!p.DataLinks.Any(l => l.DataElement?.ElementCode == "cctv.video"))
                    continue;
                var days = KvkkDurationParser.ParseMaxDays(p.RetentionText) ?? KvkkDurationParser.ParseMaxDays(p.RetentionRule?.DurationText);
                if (KvkkIntegrityRules.IsCctvOverLimit(days))
                    yield return new KvkkFindingCandidate(
                        KvkkIntegrityPatterns.CctvRetentionOver60Days, SeverityIdari, p.Id,
                        $"\"{p.Name}\" süreci CCTV görüntüsünü {days} gün saklıyor (orantılılık sınırı 60 gün).");
            }
        }

        private static IEnumerable<KvkkFindingCandidate> DetectCandidateCvOverLimit(List<KvkkProcess> processes)
        {
            foreach (var p in processes)
            {
                if (!KvkkIntegrityRules.IsCandidateProcess(p.Department, p.Name, p.Purpose))
                    continue;
                if (!p.DataLinks.Any(l => l.DataElement?.ElementCode == "hr.cv"))
                    continue;
                var days = KvkkDurationParser.ParseMaxDays(p.RetentionText) ?? KvkkDurationParser.ParseMaxDays(p.RetentionRule?.DurationText);
                if (KvkkIntegrityRules.IsCandidateCvOverLimit(days))
                    yield return new KvkkFindingCandidate(
                        KvkkIntegrityPatterns.CandidateCvRetentionOver2Years, SeverityIdari, p.Id,
                        $"\"{p.Name}\" süreci aday CV'sini {days} gün saklıyor (açık rıza sınırı 2 yıl).");
            }
        }

        private static IEnumerable<KvkkFindingCandidate> DetectHiddenCrossBorderTransfer(List<KvkkProcess> processes)
        {
            foreach (var p in processes)
                if (KvkkIntegrityRules.HasHiddenSaasTransfer(p.StorageMedium, p.CrossBorderTransfers.Count > 0))
                    yield return new KvkkFindingCandidate(
                        KvkkIntegrityPatterns.CrossBorderHiddenTransfer, SeverityKritik, p.Id,
                        $"\"{p.Name}\" süreci yurt dışı SaaS ({p.StorageMedium}) kullanıyor ama Yurt Dışı Aktarım kaydı yok.");
        }

        private static IEnumerable<KvkkFindingCandidate> DetectMissingLegalProcesses(List<KvkkProcess> processes)
        {
            var rows = processes.Select(p => (p.Department, p.Name, p.Purpose)).ToList();

            if (KvkkIntegrityRules.IsMissingLegalProcess(rows, KvkkIntegrityRules.WhistleblowerKeywords))
                yield return new KvkkFindingCandidate(
                    KvkkIntegrityPatterns.NoWhistleblowerProcess, SeverityIdari, null,
                    "Hukuk departmanında ihbar / etik hat sürecine dair kayıt yok.");

            if (KvkkIntegrityRules.IsMissingLegalProcess(rows, KvkkIntegrityRules.BreachKeywords))
                yield return new KvkkFindingCandidate(
                    KvkkIntegrityPatterns.NoBreachProcess, SeverityIdariYuksek, null,
                    "Hukuk departmanında veri ihlali yönetim sürecine dair kayıt yok.");
        }
    }
}
