using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.User
{
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public decimal TotalUsersChangePercent { get; set; }

        public int ActiveUsers { get; set; }
        public decimal ActiveUsersChangePercent { get; set; }

        public int PendingUsers { get; set; }
        public decimal PendingUsersChangePercent { get; set; }

        public int SessionUsers { get; set; }
        public decimal SessionUsersChangePercent { get; set; }
    }
}
