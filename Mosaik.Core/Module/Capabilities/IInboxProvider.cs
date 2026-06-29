namespace Mosaik.Core.Module.Capabilities;

// M2 Unified Inbox — modüller arası tek yetenek arayüzü (ADR-018, lean port).
// Her modül/alan kendi "yapılacak iş" kalemlerini bu sözleşme ile sunar.
// NOT: Bu InboxItem, Mosaik.ViewModels.Workflow.InboxItem'dan FARKLI bir tiptir.
public sealed record InboxItem
{
    // "workflow" | "obligation" | "circular" — provider kimliği (UI'da chip).
    public required string ProviderKey { get; init; }

    // "approval" | "due" | "unread" — gruplama kategorisi.
    public required string Category { get; init; }

    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public required string Url { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? DueAt { get; init; }
    public bool IsOverdue { get; init; }
}

public interface IInboxProvider
{
    string ProviderKey { get; }

    Task<IReadOnlyList<InboxItem>> GetItemsForUserAsync(
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct);
}
