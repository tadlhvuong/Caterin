using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public sealed class ServiceResult
    {
        public bool Succeeded { get; init; }
        public string? Message { get; init; }
        public object? Data { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];
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

        public static ServiceResult Fail(params string[] errors)
            => new()
            {
                Succeeded = false,
                Errors = errors
            };

        public static ServiceResult Fail(IEnumerable<string> errors)
            => new()
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
    }

    public sealed class ServiceResult<T>
    {
        public bool Succeeded { get; init; }

        public T? Data { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];

        public static ServiceResult<T> Success(T? data)
            => new()
            {
                Succeeded = true,
                Data = data
            };

        public static ServiceResult<T> Fail(params string[] errors)
            => new()
            {
                Succeeded = false,
                Errors = errors
            };

        public static ServiceResult<T> Fail(IEnumerable<string> errors)
            => new()
            {
                Succeeded = false,
                Errors = errors.ToList()
            };
    }
}
