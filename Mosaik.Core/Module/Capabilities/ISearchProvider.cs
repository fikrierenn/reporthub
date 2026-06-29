namespace Mosaik.Core.Module.Capabilities;

// M3 Cross-module Search — modüller arası tek arama yetenek arayüzü (M2 IInboxProvider ikizi, lean port).
// Her modül/alan kendi aranabilir kayıtlarını bu sözleşme ile sunar.
// GÜVENLİK: Her provider, modülünün normal listeleme akışıyla AYNI erişim/firma kapsamını uygular —
// kullanıcı arama sonucunda göremeyeceği bir kaydı asla görmemeli.
public sealed record SearchResult
{
    // "report" | "contract" | "obligation" | "circular" | "sop" — provider kimliği (UI'da chip).
    public required string ProviderKey { get; init; }

    // "Rapor" | "Sözleşme" ... — gruplama etiketi (TR, UI).
    public required string EntityLabel { get; init; }

    public required string Title { get; init; }
    public string? Snippet { get; init; }
    public required string Url { get; init; }
    public DateTime? Date { get; init; }
}

public interface ISearchProvider
{
    string ProviderKey { get; }

    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        int take,
        CancellationToken ct);
}
