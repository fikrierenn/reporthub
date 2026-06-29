using System.ComponentModel.DataAnnotations;
using Mosaik.Models;

namespace Mosaik.ViewModels
{
    // Plan 54 M4 — Dashboard→Alert eşik kuralları admin CRUD view-model'leri.

    public class EscalationRuleListViewModel
    {
        public List<EscalationRule> Rules { get; set; } = new();
        // ReportId → Başlık (liste görünümünde rapor adını göstermek için).
        public Dictionary<int, string> ReportTitles { get; set; } = new();
    }

    // Form DTO — entity direkt bind edilmez (mass-assignment koruması, M-07).
    public class EscalationRuleFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kural adı zorunlu.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Rapor seçilmeli.")]
        public int ReportId { get; set; }

        [Range(0, 50, ErrorMessage = "Result set 0-50 arası olmalı.")]
        public int ResultSet { get; set; }

        [Required(ErrorMessage = "Kolon adı zorunlu.")]
        [MaxLength(128)]
        public string Column { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Aggregation { get; set; } = "first";

        [Required]
        [MaxLength(4)]
        public string Operator { get; set; } = "gt";

        public decimal Threshold { get; set; }

        // <select multiple> name="NotifyUserIdsList" → POST'ta CSV'ye join edilir.
        public List<int> NotifyUserIdsList { get; set; } = new();

        public bool IsActive { get; set; } = true;

        // --- Form doldurma için seçenek listeleri (POST'ta kullanılmaz) ---
        public List<ReportOption> Reports { get; set; } = new();
        public List<UserOption> Users { get; set; } = new();

        public record ReportOption(int ReportId, string Title);
        public record UserOption(int UserId, string Username);
    }
}
