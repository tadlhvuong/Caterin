using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static ServiceResult Ok() => new() { Success = true };
        public static ServiceResult Fail(string msg) => new() { Success = false, Message = msg };
    }
}
