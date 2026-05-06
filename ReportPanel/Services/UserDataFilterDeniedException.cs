namespace ReportPanel.Services;

/// <summary>
/// Plan 07 Faz 4 — deny-by-default. Aktif bir FilterDefinition icin kullanicinin
/// hicbir UserDataFilters kaydi yoksa firlatilir. Caller (ReportsController) catch
/// eder, audit log yazar, HTTP 403 doner.
/// </summary>
public sealed class UserDataFilterDeniedException : Exception
{
    public string FilterKey { get; }
    public int? UserId { get; }

    public UserDataFilterDeniedException(string filterKey, int? userId)
        : base($"User {userId} is missing required data filter '{filterKey}' (deny-by-default).")
    {
        FilterKey = filterKey;
        UserId = userId;
    }
}
