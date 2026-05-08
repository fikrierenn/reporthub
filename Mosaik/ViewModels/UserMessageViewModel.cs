namespace Mosaik.ViewModels;

/// <summary>
/// Base for ViewModels that render alert/message via _AlertMessage partial.
/// MessageType: "success" | "error" | "warning" | "info" — _AlertMessage tooling expects these.
/// </summary>
public abstract class UserMessageViewModel
{
    public string Message { get; set; } = "";
    public string MessageType { get; set; } = "";
}
