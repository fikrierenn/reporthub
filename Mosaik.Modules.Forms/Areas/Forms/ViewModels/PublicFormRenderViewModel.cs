namespace Mosaik.Modules.Forms.Areas.Forms.ViewModels
{
    // Plan 41 Faz 3 — public/anonim form render.
    public sealed record PublicFormRenderViewModel(
        int FormId, string Token, string Name, string? Description, string SchemaJson);
}
