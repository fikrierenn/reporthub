using Mosaik.Modules.Kvkk.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 40 §4.3 (M6 Faz 5) — 8 pattern saf-C# karar fonksiyonları, pozitif+negatif çift.
    // Pattern 4 (disclosure-mismatch) tespit edilmiyor (DisclosureNotice yok) — test yok.
    public class KvkkIntegrityRulesTests
    {
        // Pattern 1 — kopyala-yapıştır amaç
        [Fact]
        public void IsCopyPastePurpose_NearIdenticalText_ReturnsTrue()
        {
            Assert.True(KvkkIntegrityRules.IsCopyPastePurpose(
                "Çalışan bordro ve ücret hesaplaması yapılması",
                "Çalışan bordro ve ücret hesaplamasının yapılması"));
        }

        [Fact]
        public void IsCopyPastePurpose_DifferentText_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.IsCopyPastePurpose(
                "Çalışan bordro hesaplaması",
                "Müşteri şikayet takibi ve çözümü"));
        }

        // Pattern 2 — açık rıza ↔ kanuni yükümlülük çakışması
        [Fact]
        public void IsConsentLegalConflict_PayrollWithConsent_ReturnsTrue()
        {
            Assert.True(KvkkIntegrityRules.IsConsentLegalConflict("m5-1", "İnsan Kaynakları", "Bordro Hesaplama"));
        }

        [Fact]
        public void IsConsentLegalConflict_PayrollWithLegalObligation_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.IsConsentLegalConflict("m5-2-c2", "İnsan Kaynakları", "Bordro Hesaplama"));
        }

        // Pattern 3 — CCTV > 60 gün
        [Fact]
        public void IsCctvOverLimit_Over60Days_ReturnsTrue()
        {
            Assert.True(KvkkIntegrityRules.IsCctvOverLimit(90));
        }

        [Fact]
        public void IsCctvOverLimit_UnknownOrUnder_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.IsCctvOverLimit(30));
            Assert.False(KvkkIntegrityRules.IsCctvOverLimit(null));
        }

        // Pattern 5 — aday CV > 2 yıl
        [Fact]
        public void IsCandidateProcess_NameContainsAday_ReturnsTrue()
        {
            Assert.True(KvkkIntegrityRules.IsCandidateProcess("İnsan Kaynakları", "Aday Değerlendirme", "İşe alım süreci"));
        }

        [Fact]
        public void IsCandidateProcess_UnrelatedProcess_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.IsCandidateProcess("Muhasebe", "Fatura Kesme", "Satış faturalandırma"));
        }

        [Fact]
        public void IsCandidateCvOverLimit_Over730Days_ReturnsTrue()
        {
            Assert.True(KvkkIntegrityRules.IsCandidateCvOverLimit(1000));
        }

        [Fact]
        public void IsCandidateCvOverLimit_UnderLimit_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.IsCandidateCvOverLimit(365));
        }

        // Pattern 6 — gizli yurt dışı SaaS aktarımı
        [Fact]
        public void HasHiddenSaasTransfer_KnownSaasWithoutRecord_ReturnsTrue()
        {
            Assert.True(KvkkIntegrityRules.HasHiddenSaasTransfer("Microsoft 365 SharePoint", hasCrossBorderRecord: false));
        }

        [Fact]
        public void HasHiddenSaasTransfer_RecordExists_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.HasHiddenSaasTransfer("Microsoft 365 SharePoint", hasCrossBorderRecord: true));
        }

        [Fact]
        public void HasHiddenSaasTransfer_LocalStorage_ReturnsFalse()
        {
            Assert.False(KvkkIntegrityRules.HasHiddenSaasTransfer("Lokal sunucu, on-premise", hasCrossBorderRecord: false));
        }

        // Pattern 7/8 — hukuk departmanında eksik süreç
        [Fact]
        public void IsMissingLegalProcess_NoMatchingProcess_ReturnsTrue()
        {
            var rows = new[] { ("Hukuk", "Sözleşme Yönetimi", "Sözleşme incelemesi") };
            Assert.True(KvkkIntegrityRules.IsMissingLegalProcess(rows, KvkkIntegrityRules.WhistleblowerKeywords));
        }

        [Fact]
        public void IsMissingLegalProcess_MatchingProcessExists_ReturnsFalse()
        {
            var rows = new[] { ("Hukuk", "Etik Hat Başvuru Yönetimi", "İhbar değerlendirme") };
            Assert.False(KvkkIntegrityRules.IsMissingLegalProcess(rows, KvkkIntegrityRules.WhistleblowerKeywords));
        }
    }
}
