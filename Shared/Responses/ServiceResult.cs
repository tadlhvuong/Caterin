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

        public object? Data { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];

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

        //public static ServiceResult Success<T>(T data)
        //=> new()
        //{
        //    Succeeded = true,
        //    Data = data
        //};
        //public static ServiceResult Fail<T>(params string[] errors)
        //    => new()
        //    {
        //        Succeeded = false,
        //        Errors = errors.ToList()
        //    };
    }
}
