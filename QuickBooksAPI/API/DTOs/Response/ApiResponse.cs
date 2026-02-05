namespace QuickBooksAPI.API.DTOs.Response
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;        
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        // PRIMARY fail method (frontend-focused)
        public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message
            };
        }

        // OPTIONAL: detailed errors (logging / advanced UI)
        public static ApiResponse<T> Fail(string message, IEnumerable<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }

    }

}
