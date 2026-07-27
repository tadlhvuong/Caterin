using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email.EmailModels
{
    public class ConfirmEmailModel : BaseEmailModel
    {
        public string ConfirmEmailUrl { get; set; } = default!;
        public int ExpireHours { get; set; }
    }
}
