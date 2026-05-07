using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Plan 17 Faz F — AI özet/analiz için Admin UI'dan yönetilen runtime config.
    // Singleton: tek satır kayıt (BrandSettings pattern).
    //
    // Provider:
    //   - "groq"   → Llama 3.3 70B / 3.1 8B (free tier 14400 req/gün)
    //   - "gemini" → gemini-1.5-flash (free 1500 req/gün)
    //   - "ollama" → local (BaseUrl http://localhost:11434)
    //   - "openai" → gpt-4o-mini vb. (paid)
    public class AiSettings : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // Profil etiketi (örn. "Groq Production", "Gemini Backup") — UI tanıma için
        [MaxLength(50)]
        public string? Name { get; set; }

        [Required]
        [MaxLength(20)]
        public string Provider { get; set; } = "groq";

        [MaxLength(500)]
        public string? ApiKey { get; set; }

        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = "llama-3.3-70b-versatile";

        public int MaxTokens { get; set; } = 1024;

        public double Temperature { get; set; } = 0.3;

        [MaxLength(255)]
        public string? BaseUrl { get; set; }

        public bool IsEnabled { get; set; } = false;

        // Birincil provider (true ise denemenin başında bu kullanılır).
        // Sadece bir kayıt IsPrimary=true olmalı (controller garanti eder).
        public bool IsPrimary { get; set; } = false;

        // Fallback sırası (düşük = önce). IsPrimary=true olan ilk denenir,
        // başarısız olursa kalan IsEnabled=true rows Priority asc sıralanır.
        public int Priority { get; set; } = 100;

        // Son test sonucu (Admin UI'da göster)
        [MaxLength(500)]
        public string? LastTestMessage { get; set; }

        public DateTime? LastTestAt { get; set; }

        public bool LastTestSuccess { get; set; }
    }
}
