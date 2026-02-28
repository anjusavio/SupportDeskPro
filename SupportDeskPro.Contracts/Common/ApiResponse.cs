namespace SupportDeskPro.Contracts.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public PaginationMeta? Pagination { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> Fail(string message,
            Dictionary<string, string[]>? errors = null) => new()
            {
                Success = false,
                Message = message,
                Errors = errors
            };
    }

    public class PaginationMeta
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
