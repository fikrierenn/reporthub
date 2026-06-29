using Mosaik.Core.Module.Capabilities;
using Mosaik.Services.Workflow;

namespace Mosaik.Services.Inbox;

// M2 — mevcut WorkflowInboxService'i sarar (onu BOZMA, sadece map et).
// Kullanıcıya atanmış aktif onay step'lerini Capabilities.InboxItem'a çevirir.
public sealed class WorkflowInboxProvider(WorkflowInboxService workflowInbox) : IInboxProvider
{
    private readonly WorkflowInboxService _workflowInbox = workflowInbox;

    public string ProviderKey => "workflow";

    public async Task<IReadOnlyList<InboxItem>> GetItemsForUserAsync(
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct)
    {
        var pending = await _workflowInbox.GetPendingForUserAsync(userId, roles, firmaIds, limit: null, ct: ct);
        if (pending.Count == 0) return Array.Empty<InboxItem>();

        return pending.Select(p => new InboxItem
        {
            ProviderKey = "workflow",
            Category = "approval",
            Title = p.TemplateName,
            Subtitle = string.IsNullOrWhiteSpace(p.EntityTitle)
                ? p.StepLabel
                : $"{p.StepLabel} · {p.EntityTitle}",
            Url = string.IsNullOrWhiteSpace(p.EntityUrl)
                ? $"/Workflow/Instance/{p.InstanceId}"
                : p.EntityUrl,
            CreatedAt = p.StartedAt
        }).ToList();
    }
}
