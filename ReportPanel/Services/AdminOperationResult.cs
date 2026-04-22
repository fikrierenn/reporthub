namespace ReportPanel.Services
{
    /// <summary>
    /// M-01: Admin servislerinden donen standard sonuc.
    /// Controller TempData'ya Message + MessageType ile eşleştirir.
    /// </summary>
    public readonly record struct AdminOperationResult(bool Success, string Message)
    {
        public static AdminOperationResult Ok(string message) => new(true, message);
        public static AdminOperationResult Fail(string message) => new(false, message);

        public string TempDataType => Success ? "success" : "error";
    }
}
