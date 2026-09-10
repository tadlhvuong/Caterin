namespace Shared.Responses
{
    public sealed class ServiceResult
    {
        public bool Succeeded { get; init; }

        public string? Message { get; init; }

        public object? Data { get; init; }

        public IReadOnlyList<ServiceError> Errors { get; init; } = [];

        public static ServiceResult SuccessMessage(string message)
            => new()
            {
                Succeeded = true,
                Message = message
            };

        public static ServiceResult Success(object? data = null)
            => new()
            {
                Succeeded = true,
                Data = data
            };

        // Fail("error")
        public static ServiceResult Fail(params string[] errors)
            => new()
            {
                Succeeded = false,
                Errors = errors
                    .Select(x => new ServiceError
                    {
                        Message = x
                    })
                    .ToList()
            };
        public static ServiceResult Fail(IEnumerable<string> errors)
       => new()
       {
           Succeeded = false,
           Errors = errors
               .Select(x => new ServiceError
               {
                   Message = x
               })
               .ToList()
       };
        public static ServiceResult Fail(IEnumerable<ServiceError> errors)
            => new()
            {
                Succeeded = false,
                Errors = errors.ToList()
            };

        public static ServiceResult Fail(params ServiceError[] errors)
            => new()
            {
                Succeeded = false,
                Errors = errors
            };
    }


    public sealed class ServiceResult<T>
    {
        public bool Succeeded { get; init; }

        public T? Data { get; init; }
        public string? Message { get; set; }

        public IReadOnlyList<ServiceError> Errors { get; init; } = [];

        public static ServiceResult<T> Success(T? data = default)
            => new()
            {
                Succeeded = true,
                Data = data,
            };

        public static ServiceResult<T> Success(T? data = default, string? message = default)
            => new()
            {
                Succeeded = true,
                Data = data,
                Message = message
            };

        // Fail("error")
        public static ServiceResult<T> Fail(params string[] errors)
            => new()
            {
                Succeeded = false,
                Errors = errors
                    .Select(x => new ServiceError
                    {
                        Message = x
                    })
                    .ToList()
            };

        // Fail(IEnumerable<string>)
        public static ServiceResult<T> Fail(IEnumerable<ServiceError> errors)
            => new()
            {
                Succeeded = false,
                Errors = errors.ToList()
            };

        // Fail(ServiceError)
        // Fail(ServiceError, ServiceError)
        public static ServiceResult<T> Fail(params ServiceError[] errors)
            => new()
            {
                Succeeded = false,
                Errors = errors
            };
    }


    public sealed class ServiceError
    {
        public string? Field { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
