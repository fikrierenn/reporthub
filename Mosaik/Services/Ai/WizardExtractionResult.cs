namespace Mosaik.Services.Ai
{
    // Plan 25 wizard — AI Stage1 promptunun JSON çıktısı bu record'a parse edilir.
    // Frontend wizard.js applyAiResult() ile form alanlarına populate eder.
    // Tüm alanlar nullable: AI bulamadıysa null bırakır.
    public sealed record WizardExtractionResult(
        string? Title,
        string? Counterparty,
        int? Category,         // ContractCategory enum int değeri
        string? StartDate,     // ISO yyyy-MM-dd veya null
        string? EndDate,
        string? Notes,
        IReadOnlyList<WizardObligationSuggestion>? Obligations
    );

    public sealed record WizardObligationSuggestion(
        string? Title,
        decimal? Amount,
        string? Currency,
        bool IsRecurring,
        int? RecurrenceType,    // 0=Monthly, 1=Quarterly, 2=Yearly, 3=Custom
        int? DayOfMonth,
        int? MonthOfYear,
        string? DueDate,        // tek seferlikse ISO
        string? Notes
    );
}
