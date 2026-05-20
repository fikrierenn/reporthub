namespace Mosaik.Core.Intelligence
{
    // Plan 38 — polymorphic entity type whitelist.
    // EntityRelations.SourceType / TargetType + DecisionLog.RelatedEntityType
    // bu sabitlerden birini alır. Yeni modül eklenirken buraya satır eklenir.
    public static class EntityType
    {
        public const string User = "User";
        public const string Department = "Department";
        public const string Position = "Position";
        public const string Contract = "Contract";
        public const string Obligation = "Obligation";
        public const string Vendor = "Vendor";
        public const string Document = "Document";
        public const string Circular = "Circular";
        public const string WorkflowInstance = "WorkflowInstance";
        public const string Report = "Report";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            User, Department, Position, Contract, Obligation,
            Vendor, Document, Circular, WorkflowInstance, Report
        };

        public static bool IsValid(string? type) => type is not null && All.Contains(type);
    }
}
