namespace Mosaik.ViewModels.Ui
{
    /// <summary>
    /// _EmptyState.cshtml partial için model.
    /// Detay: .claude/rules/ui-patterns.md §5
    /// </summary>
    public class EmptyStateViewModel
    {
        public string Icon { get; set; } = "fa-folder-open";
        public string Title { get; set; } = "Henüz kayıt yok";
        public string? Message { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }

    /// <summary>
    /// _FormActionRow.cshtml partial için model.
    /// Detay: .claude/rules/ui-patterns.md §8
    /// </summary>
    public class FormActionRowViewModel
    {
        public string BackUrl { get; set; } = "";
        public string BackText { get; set; } = "Geri Dön";
        public string SubmitText { get; set; } = "Kaydet";
        public string SubmitIcon { get; set; } = "fa-check";
        public string MaxWidth { get; set; } = "880px";
    }

    /// <summary>
    /// _FilterBar.cshtml partial için model.
    /// Detay: .claude/rules/ui-patterns.md §4
    /// </summary>
    public class FilterBarViewModel
    {
        public string FormAction { get; set; } = "";
        public string SearchParamName { get; set; } = "q";
        public string SearchPlaceholder { get; set; } = "Ara...";
        public string? SearchValue { get; set; }
        public List<FilterBarSelect> Selects { get; set; } = new();
    }

    public class FilterBarSelect
    {
        public string ParamName { get; set; } = "";
        public string Label { get; set; } = "";
        public string? SelectedValue { get; set; }
        public List<FilterBarOption> Options { get; set; } = new();
    }

    public class FilterBarOption
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
