namespace OrderService.Domain.Responses
{
    public class BaseResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int ErrorCode { get; set; }

        // 👇 Public parameterless constructor (needed for serialization)
        public BaseResponse() { }

        // 👇 Internal constructor used by factory methods
        private BaseResponse(string message, T? data, int errorCode)
        {
            Message = message;
            Data = data;
            ErrorCode = errorCode;
        }

        // Success response
        public static BaseResponse<T> Ok(T data, string message = "Operation successful")
            => new BaseResponse<T>(message, data, 0);

        // Failure response
        public static BaseResponse<T> Fail(string message, int errorCode = 1)
            => new BaseResponse<T>(message, default, errorCode);
    }
}
