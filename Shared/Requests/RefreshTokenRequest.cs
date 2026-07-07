using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests
{
    public class RefreshTokenRequest 
    { 
        [Required] 
        public string AccessToken { get; set; } = default!; 
        [Required] 
        public string RefreshToken { get; set; } = default!; }
}
