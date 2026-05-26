namespace Mosaik.Core.AI.Rag
{
    // Plan 44 — RAG retrieval için kullanıcı izin bağlamı.
    // SopChunkRetriever.SearchAsync + gelecekteki DocumentChunkRetriever ortak kullanır.
    // SecurityClearance: 0=public, 1=internal, 2=confidential, 3=restricted (User tablosundan veya role map'den)
    public record RagUserContext(
        int UserId,
        int FirmaId,
        byte SecurityClearance,
        IReadOnlyList<string> RoleNames,
        IReadOnlyList<int> DepartmentIds
    )
    {
        // Role → default clearance haritası.
        // Explicit User.SecurityClearance yoksa bu map kullanılır.
        public static byte ClearanceFromRoles(IEnumerable<string> roles)
        {
            byte max = 1; // default internal
            foreach (var role in roles)
            {
                var c = role.ToLowerInvariant() switch
                {
                    "admin"   => (byte)3,
                    "hr"      => (byte)2,
                    "finans"  => (byte)2,
                    "manager" => (byte)2,
                    "guest"   => (byte)0,
                    _         => (byte)1
                };
                if (c > max) max = c;
            }
            return max;
        }
    }
}
