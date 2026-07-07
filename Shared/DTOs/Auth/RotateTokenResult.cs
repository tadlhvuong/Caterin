using Shared.Data.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Data.Entities.Identity;

namespace Shared.DTOs
{
    public class RotateTokenResult
    {
        public string RefreshToken { get; set; } = default!;

        public RefreshToken RefreshTokenEntity { get; set; } = default!;

        public AppUser User { get; set; } = default!;
    }
}
