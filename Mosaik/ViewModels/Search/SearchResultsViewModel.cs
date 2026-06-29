using Mosaik.Core.Module.Capabilities;

namespace Mosaik.ViewModels.Search;

// M3 Cross-module Search — arama sonuç sayfası view-model'i.
public sealed class SearchResultsViewModel
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<SearchResult> Results { get; init; } = Array.Empty<SearchResult>();
}
