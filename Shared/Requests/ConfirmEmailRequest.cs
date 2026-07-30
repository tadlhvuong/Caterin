using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests
{

    public sealed class ConfirmEmailRequest
    {
        public string Key { get; set; } = string.Empty;
    }
}
