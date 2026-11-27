namespace Qresto.Helpers;

// Loi nghiep vu tra ve cho nguoi dung (khong phai loi he thong).
// Code dung cho JavaScript phia khach biet phai xu ly the nao.
public class BusinessException : Exception
{
    public string Code { get; }
    public object? Payload { get; }

    public BusinessException(string code, string message, object? data = null) : base(message)
    {
        Code = code;
        Payload = data;
    }
}

public static class ErrorCodes
{
    public const string InvalidInput = "InvalidInput";
    public const string TableNotFound = "TableNotFound";
    public const string SessionClosed = "SessionClosed";     // ban dang cho thanh toan
    public const string StaleSession = "StaleSession";       // trang thuoc luot khach cu
    public const string NoAccess = "NoAccess";
    public const string ItemUnavailable = "ItemUnavailable";
    public const string PriceChanged = "PriceChanged";
    public const string DuplicateMismatch = "DuplicateMismatch";
    public const string Conflict = "Conflict";               // du lieu vua bi nguoi khac doi
    public const string NotAllowed = "NotAllowed";
    public const string NotFound = "NotFound";
}
