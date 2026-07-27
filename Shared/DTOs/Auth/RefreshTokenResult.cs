using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Auth
{
    public class RefreshTokenResult
    {
        public string RefreshToken { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
