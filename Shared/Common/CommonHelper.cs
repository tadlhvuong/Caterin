using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Common
{
    public static class CommonHelper
    {
        static Random rdm = new Random();
        //static DateTime utcStart = new DateTime(1970, 1, 1);
        public static readonly char[] LineDelimiters = new char[] { '\r', '\n' };

        public static string GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        public static string GetUserAvatar(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            //add claim avatar in controller login
            return principal.FindFirst("Avatar")?.Value;
        }
        public static string Hash(string key)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(key));

            return Convert.ToHexString(bytes);
        }

        public static string Generate(int byteLength = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes);
        }
        public static string NormalizeRoute(string route)
        {
            return route
                .Trim('/')
                .ToLowerInvariant();
        }

        public static string GET_IP()
        {
            //https://api.ipify.org or http://checkip.dyndns.org/
            var pubIp = IPResquesHelper("https://api.ipify.org");
            if (pubIp == null) return "";
            return pubIp;
        }


        public static string GET_Location()
        {
            var ip = GET_IP();
            var ipresponse = IPResquesHelper("http://demo.ip-api.com/json/" + ip);
            return ipresponse;
        }
        public static string IPResquesHelper(string url)
        {
            HttpWebRequest objrequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse objresponse = (HttpWebResponse)objrequest.GetResponse();
            StreamReader responsereader = new StreamReader(objresponse.GetResponseStream());
            string responseread = responsereader.ReadToEnd();
            responsereader.Close();
            responsereader.Dispose();
            return responseread;
        }
    }
}
