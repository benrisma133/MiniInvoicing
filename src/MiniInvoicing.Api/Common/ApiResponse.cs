namespace MiniInvoicing.Api.Common;

public class ApiResponse<T>
{
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(string message, T? data = default)
    {
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> SuccessResponse(string message, T data)
    {
        return new ApiResponse<T>(message, data);
    }

    public static ApiResponse<T> ErrorResponse(string message)
    {
        return new ApiResponse<T>(message, default);
    }
}