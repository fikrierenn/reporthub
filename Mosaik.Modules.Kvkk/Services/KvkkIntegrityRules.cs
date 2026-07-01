using FuzzySharp;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 §4.3 (M6 Faz 5) — 8 saf-C# integrity pattern kimliği + saf/testable karar fonksiyonları.
    // DB erişimi KvkkIntegrityChecker'da; burada sadece pure predicate (unit test edilebilir).
    public static class KvkkIntegrityPatterns
    {
        public const string CopyPastePurpose = "copy-paste-purpose";
        public const string ConsentLegalConflict = "consent-legal-conflict";
        public const string CctvRetentionOver60Days = "cctv-retention-over-60";
        public const string DisclosureMismatch = "disclosure-mismatch";       // Faz 5'te tespit edilmiyor (DisclosureNotice yok, Faz 6 ertelendi)
        public const string CandidateCvRetentionOver2Years = "candidate-cv-retention-over-2y";
        public const string CrossBorderHiddenTransfer = "cross-border-hidden";
        public const string NoWhistleblowerProcess = "no-whistleblower-process";
        public const string NoBreachProcess = "no-breach-process";
        // Plan 40 Faz 3 — SOP taranan veri öğesi, bağlı süreçte tanımlı değil.
        public const string SopDataElementGap = "sop-dataelement-gap";
    }

    public static class KvkkIntegrityRules
    {
        private const int FuzzyPurposeThreshold = 85;
        private const int CctvMaxDays = 60;
        private const int CandidateCvMaxDays = 730; // 2 yıl

        private static readonly string[] LegalObligationKeywords =
            { "bordro", "sgk", "özlük", "maaş", "ücret", "puantaj" };

        private static readonly string[] CandidateKeywords = { "aday" };

        private static readonly string[] HiddenSaasKeywords =
            { "microsoft 365", "office 365", "google", "aws", "amazon web", "azure", "workspace" };

        // Pattern 1 — 2 sürecin Purpose'u fuzzy >85% benzer mi (kopyala-yapıştır işaret).
        public static bool IsCopyPastePurpose(string purposeA, string purposeB)
        {
            if (string.IsNullOrWhiteSpace(purposeA) || string.IsNullOrWhiteSpace(purposeB))
                return false;
            return Fuzz.TokenSetRatio(purposeA, purposeB) >= FuzzyPurposeThreshold;
        }

        // Pattern 2 — özlük/bordro/SGK departmanında hukuki yükümlülük yerine "açık rıza" (m5-1) seçilmiş mi.
        public static bool IsConsentLegalConflict(string legalBasisCode, string department, string processName)
        {
            if (!string.Equals(legalBasisCode, "m5-1", StringComparison.OrdinalIgnoreCase))
                return false;
            var haystack = $"{department} {processName}".ToLowerInvariant();
            return LegalObligationKeywords.Any(haystack.Contains);
        }

        // Pattern 3 + Pattern 5 ortak — parse edilmiş süre eşiği aşıyor mu (bilinmeyen süre = false, false-positive yok).
        public static bool IsRetentionOverDays(int? actualMaxDays, int thresholdDays) =>
            actualMaxDays.HasValue && actualMaxDays.Value > thresholdDays;

        public static bool IsCctvOverLimit(int? actualMaxDays) => IsRetentionOverDays(actualMaxDays, CctvMaxDays);

        public static bool IsCandidateProcess(string department, string processName, string purpose)
        {
            var haystack = $"{department} {processName} {purpose}".ToLowerInvariant();
            return CandidateKeywords.Any(haystack.Contains);
        }

        public static bool IsCandidateCvOverLimit(int? actualMaxDays) =>
            IsRetentionOverDays(actualMaxDays, CandidateCvMaxDays);

        // Pattern 6 — StorageMedium bilinen yurt dışı SaaS'a işaret ediyor ama CrossBorderTransfer kaydı yok.
        public static bool HasHiddenSaasTransfer(string? storageMedium, bool hasCrossBorderRecord)
        {
            if (hasCrossBorderRecord || string.IsNullOrWhiteSpace(storageMedium))
                return false;
            var haystack = storageMedium.ToLowerInvariant();
            return HiddenSaasKeywords.Any(haystack.Contains);
        }

        // Pattern 7/8 — Hukuk departmanı süreçleri arasında ilgili anahtar kelime geçen SÜREÇ yok mu.
        public static bool IsMissingLegalProcess(
            IEnumerable<(string Department, string Name, string Purpose)> processes, string[] keywords)
        {
            var legalRows = processes.Where(p =>
                p.Department.Contains("hukuk", StringComparison.OrdinalIgnoreCase)).ToList();
            if (legalRows.Count == 0)
                return true; // hukuk departmanı hiç yoksa da eksik say
            return !legalRows.Any(p =>
            {
                var haystack = $"{p.Name} {p.Purpose}".ToLowerInvariant();
                return keywords.Any(haystack.Contains);
            });
        }

        public static readonly string[] WhistleblowerKeywords = { "ihbar", "whistleblower", "etik hat" };
        public static readonly string[] BreachKeywords = { "ihlal", "breach" };
    }
}
