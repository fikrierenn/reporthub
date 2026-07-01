namespace Mosaik.Core.Intelligence
{
    // Plan 38 — EntityRelations.RelationType whitelist.
    // İlk faz: 8 tip. Genişletmek için buraya sabit ekle.
    public static class RelationType
    {
        public const string Manages = "manages";
        public const string MemberOf = "member_of";
        public const string Owns = "owns";
        public const string Signed = "signed";
        public const string AssignedTo = "assigned_to";
        public const string References = "references";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        // Plan 54 M6 / Plan 40 — süreç bir veri öğesini işler
        public const string Processes = "processes";
        // Plan 40 Faz 3 — SOP prosedürü süreçten türer (Source=Sop, Target=KvkkProcess)
        public const string DerivedFrom = "derived_from";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Manages, MemberOf, Owns, Signed, AssignedTo, References, Approved, Rejected, Processes, DerivedFrom
        };

        public static bool IsValid(string? type) => type is not null && All.Contains(type);
    }
}
