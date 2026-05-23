using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.ViewModels
{
    // Plan 34.1 Faz 4 A-20 — AdvisorController.Index view model.
    public class SopAdvisorViewModel
    {
        public List<SopAdvisorHistoryItem> History { get; set; } = new();
        public int RemainingQuota { get; set; }
        public bool IsAdmin { get; set; }
        public string? SopHint { get; set; }
    }

    // Plan 34.1 Faz 5 A-27 — Admin feedback review tablo satırı.
    public class SopAdvisorFeedbackRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? FeedbackNote { get; set; }
        public string? SourceVersionIds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FeedbackAt { get; set; }
    }

    public class SopAdvisorHistoryItem
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        // Lookup sopAiFeedback: 0 None | 1 ThumbsUp | 2 ThumbsDown
        public byte Feedback { get; set; }
        public List<int> SourceVersionIds { get; set; } = new();

        public static SopAdvisorHistoryItem From(SopAiConversation c) => new()
        {
            Id = c.Id,
            Question = c.Question,
            Answer = c.Answer,
            CreatedAt = c.CreatedAt,
            Feedback = c.UserFeedback,
            SourceVersionIds = string.IsNullOrWhiteSpace(c.SourceSopVersionIds)
                ? new()
                : c.SourceSopVersionIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s, out var i) ? i : 0)
                    .Where(i => i > 0)
                    .ToList()
        };
    }
}
