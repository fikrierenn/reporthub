namespace ReportPanel.ViewModels;

/// <summary>
/// Reusable password input partial için view-model.
/// Plan 04 F2.3 (28 Nisan 2026): Login + Profile şifre alanları DRY için.
/// </summary>
public class PasswordInputViewModel
{
    public string Id { get; set; } = "Password";
    public string Name { get; set; } = "Password";
    public string Label { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public string Autocomplete { get; set; } = "off";
    public bool Required { get; set; } = false;
}
