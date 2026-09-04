using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Common
{
    public static class CommonHelper
    {
        static Random rdm = new Random();
        //static DateTime utcStart = new DateTime(1970, 1, 1);
        public static readonly char[] LineDelimiters = new char[] { '\r', '\n' };

        public static float RandomF()
        {
            return (float)(2 * rdm.NextDouble() - 1);
        }

        public static int Random()
        {
            return rdm.Next(0, int.MaxValue);
        }

        public static int Random(int maxValue)
        {
            return rdm.Next(0, maxValue);
        }

        public static int Random(int minValue, int maxValue)
        {
            return rdm.Next(minValue, maxValue);
        }

        public static string Random_Mix(int _size)
        {
            byte[] res = new byte[_size];
            byte achar = 0;
            for (int i = 0; i < _size; i++)
            {
                do
                {
                    achar = (byte)(rdm.Next(64) + 48);
                } while (((achar < 50) || (achar > 57)) && ((achar < 65) || (achar > 90) || (achar == 73) || (achar == 79)));
                res[i] = achar;
            }

            return Encoding.ASCII.GetString(res);
        }

        public static string Random_Num(int _size)
        {
            byte[] res = new byte[_size];
            byte achar = 0;
            for (int i = 0; i < _size; i++)
            {
                do
                {
                    achar = (byte)(rdm.Next(64) + 48);
                } while ((achar < 48) || (achar > 57));
                res[i] = achar;
            }

            return Encoding.ASCII.GetString(res);
        }
        public static class OtpGenerator
        {
            public static string Generate(int length = 6)
            {
                int max = (int)Math.Pow(10, length);

                int number = RandomNumberGenerator.GetInt32(max);

                return number.ToString($"D{length}");
            }
        }
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

        public static string GenerateSecureToken(int byteLength = 32)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
        public static string NormalizeRoute(string route)
        {
            return route
                .Trim('/')
                .ToLowerInvariant();
        }

        public static string NormalizeVietnamese(string orgText)
        {
            string newText = orgText.Normalize(NormalizationForm.FormD);

            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            newText = regex.Replace(newText, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D').Replace('\u0020', '-');
            newText = Regex.Replace(newText, "[^0-9a-zA-Z_-]+", "");
            return newText;
        }
        public static string ConvertEmailToName(string email)
        {
            var index = email.IndexOf('@');
            var usernmae = email.Substring(0, index);
            return email.Substring(0, index);
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

        public static class SlugHelper
        {
            public static string Generate(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return string.Empty;

                text = text.Trim().ToLowerInvariant();

                text = RemoveVietnameseCharacters(text);

                text = Regex.Replace(
                    text,
                    @"[^a-z0-9\s-]",
                    "");

                text = Regex.Replace(
                    text,
                    @"[\s-]+",
                    "-");

                return text.Trim('-');
            }

            private static string RemoveVietnameseCharacters(
                string text)
            {
                var normalized = text.Normalize(
                    NormalizationForm.FormD);

                var chars = normalized
                    .Where(c =>
                        CharUnicodeInfo.GetUnicodeCategory(c)
                        != UnicodeCategory.NonSpacingMark)
                    .ToArray();

                return new string(chars)
                    .Normalize(NormalizationForm.FormC)
                    .Replace('đ', 'd');
            }
        }
    }
}
