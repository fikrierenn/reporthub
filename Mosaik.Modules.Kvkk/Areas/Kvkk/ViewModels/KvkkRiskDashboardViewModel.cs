namespace Mosaik.Modules.Kvkk.Areas.Kvkk.ViewModels
{
    // Plan 40 (M6) Faz 7 — Risk Dashboard (xlsx "Risk Özeti" sheet parite).
    public sealed record DepartmentRiskRow(string Department, int Low, int Mid, int High);
    public sealed record TopDataElementRow(string DisplayName, int Count);
    public sealed record SeverityCount(byte Severity, int Count);

    public sealed record KvkkRiskDashboardViewModel(
        List<DepartmentRiskRow> DepartmentRisk,
        List<TopDataElementRow> TopDataElements,
        List<SeverityCount> OpenFindingsBySeverity,
        int TotalProcesses,
        int TotalOpenFindings);
}
