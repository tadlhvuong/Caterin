using Microsoft.AspNetCore.Identity;
using Shared.Data.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Authentication
{
    public class UserProfileService
    {
        public string GetAvatar(AppUser user, IList<UserLoginInfo> logins)
        {
            if (!string.IsNullOrEmpty(user.Avatar))
                return user.Avatar;

            var fb = logins.FirstOrDefault(x => x.LoginProvider == "Facebook");

            if (fb == null)
                return "/img/default-avatar-male.webp";

            return $"https://graph.facebook.com/{fb.ProviderKey}/picture?type=large";
        }

        public string GetFacebookUrl(AppUser user, IList<UserLoginInfo> logins)
        {
            if (logins == null)
                return null;

            var fbLogin = logins.FirstOrDefault(x => x.LoginProvider == "Facebook");
            if (fbLogin == null)
                return null;

            return string.Format("https://www.facebook.com/app_scoped_user_id/{0}", fbLogin.ProviderKey);
        }
    }
}
