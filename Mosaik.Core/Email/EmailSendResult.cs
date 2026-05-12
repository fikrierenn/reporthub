namespace Mosaik.Core.Email
{
    // Plan 31 — Email gönderim sonucu. Caller başarısızlığı görmeli (silent-failure-hunter).
    // `ErrorDetail` sadece log için; UI'a göstermek YASAK (security-principles §7).
    public sealed record EmailSendResult(
        bool IsSuccess,
        bool WasSkipped,
        string? ErrorCode,
        string? ErrorDetail)
    {
        public static EmailSendResult Ok() => new(true, false, null, null);

        public static EmailSendResult Skipped(string reason) =>
            new(false, true, "skipped", reason);

        public static EmailSendResult Failed(string code, string? detail = null) =>
            new(false, false, code, detail);
    }

    // Plan 31 — Bulk gönderim partial failure özetı.
    public sealed record EmailBulkResult(
        int Sent,
        int Skipped,
        IReadOnlyList<EmailBulkFailure> Failures)
    {
        public bool AllSucceeded => Failures.Count == 0 && Sent > 0;
    }

    public sealed record EmailBulkFailure(string Recipient, string ErrorCode);
}
