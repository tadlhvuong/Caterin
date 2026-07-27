using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email.EmailModels
{
    public class SuccessEmailModel : BaseEmailModel
    {
        public string LoginUrl { get; set; }

        public string NewEmail { get; set; }
    }
}
