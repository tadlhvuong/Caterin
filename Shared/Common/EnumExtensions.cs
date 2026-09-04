using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType()
                .GetMember(value.ToString())
                .FirstOrDefault();

            var attribute = member?
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();

            return attribute?.ShortName
                ?? attribute?.Name
                ?? value.ToString();
        }

        public static string GetStatusCss(this Enum value)
        {
            var member = value.GetType()
                .GetMember(value.ToString())
                .FirstOrDefault();

            var attribute = member?
                .GetCustomAttributes(typeof(StatusCssAttribute), false)
                .Cast<StatusCssAttribute>()
                .FirstOrDefault();

            return attribute?.Name ?? "secondary";
        }
    }
}
