namespace Mosaik.Core.AI.Rag
{
    // Plan 44 — Cross-modül chunk erişim politikası.
    // SQL-side retrieval sonrası in-memory defense-in-depth check.
    // SOP / Document / Contract retriever'ları ortak kullanır.
    public interface IRagAccessPolicy
    {
        // Chunk erişilebilir mi? True = LLM context'e alınabilir.
        bool IsAccessible(
            byte chunkSecurityLevel,
            string? allowedRoleIds,
            string? allowedDepartmentIds,
            string? allowedUserIds,
            RagUserContext user);
    }
}
