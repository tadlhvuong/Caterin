using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email.EmailModels
{
    public abstract class BaseEmailModel
    {
        /// <summary>
        /// Logo website
        /// </summary>
        public string LogoUrl { get; set; } = $"https://drive.google.com/file/d/1aPrIUyCHXox7UZyGz_76n5nUMnWCqo_P/view";

        /// <summary>
        /// Tên website
        /// </summary>
        public string SiteName { get; set; } = "caterin.vn";

        /// <summary>
        /// Hotline hỗ trợ
        /// </summary>
        public string SupportPhone { get; set; } = "0903653303";

        /// <summary>
        /// Tên người nhận
        /// </summary>
        public string UserName { get; set; } 
    }
}
