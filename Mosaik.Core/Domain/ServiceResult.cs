namespace Mosaik.Core.Domain
{
    // YonetIQ port (security #7 fix: ex.Message expose YASAK, generic mesaj).
    // Servis katmanı bunu döner, controller try-catch yerine IsSuccess kontrolü.
    public class ServiceResult
    {
        public bool IsSuccess { get; protected set; }
        public string Message { get; protected set; } = string.Empty;
        public string? ErrorCode { get; protected set; }

        public static ServiceResult Ok(string message = "") =>
            new() { IsSuccess = true, Message = message };

        public static ServiceResult Failure(string message, string? errorCode = null) =>
            new() { IsSuccess = false, Message = message, ErrorCode = errorCode };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; protected set; }

        public static ServiceResult<T> Ok(T data, string message = "") =>
            new() { IsSuccess = true, Data = data, Message = message };

        public static new ServiceResult<T> Failure(string message, string? errorCode = null) =>
            new() { IsSuccess = false, Message = message, ErrorCode = errorCode };
    }
}
