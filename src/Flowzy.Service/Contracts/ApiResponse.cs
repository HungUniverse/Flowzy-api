namespace Flowzy.Service.Contracts;

public sealed record ApiResponse<T>(int Code, string Message, T? Data)
{
    public static ApiResponse<T> Success(T? data, string message = "Success") => new(200, message, data);
    public static ApiResponse<T> Error(int code, string message, T? data = default) => new(code, message, data);
}
