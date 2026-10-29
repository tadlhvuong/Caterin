using Microsoft.AspNetCore.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.Data.Entities.Media;
using Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Shared.Data.Entities.Identity
{
    public class AppUser : IdentityUser
    {
        public string? TenantId { get; set; }
        public string? Avatar { get; set; }

        public long? AvatarMediaFileId { get; set; }

        [Display(Name = "Đã xóa")]
        public bool IsDeleted { get; set; } = false;
        [Display(Name = "Đã khóa")]
        public bool IsLocked { get; set; } = false;

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
        public long PermissionVersion { get; set; } = 1;
        public int TokenVersion { get; set; } = 1;

        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public Gender? Gender { get; set; }

        [Display(Name = "Địa chỉ chi tiết")]
        [StringLength(255)]
        public string? AddressDetail { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        [StringLength(20)]
        public string? ProvinceCode { get; set; }

        [Display(Name = "Phường/Xã")]
        [StringLength(20)]
        public string? WardCode { get; set; }

        [Display(Name = "Trạng thái")]
        public EntityStatus Status { get; set; }

        [Display(Name = "Thời gian tạo")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm}")]
        public DateTime CreatedAt { get; set; }
        [StringLength(128)]
        [Display(Name = "Người tạo")]
        public string? CreatedById { get; set; }

        [Display(Name = "Thời gian tạo")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm}")]
        public DateTime? UpdatedAt { get; set; }
        [StringLength(128)]
        [Display(Name = "Người tạo")]
        public string? UpdatedById { get; set; }

        public AppUser? UpdatedByUser { get; set; } = null!;

        [Display(Name = "IP tạo")]
        [StringLength(60)]
        public string? CreatedIP { get; set; }

        [Display(Name = "LastLogin")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm}")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "IPLogin")]
        [StringLength(60)]
        public string? LastLoginIP { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = null!;

        public virtual ICollection<EmailAction> EmailActions { get; set; } = [];
        public virtual ICollection<AppUserRole> UserRoles { get; set; } = [];
        public ICollection<UserSocialLink> SocialLinks { get; set; } = [];

        public virtual MediaFile? AvatarMediaFile { get; set; }
        //public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
        //[NotMapped]
        //public virtual ICollection<UserLoginInfo> ExtLogins { get; set; }

        //[NotMapped]
        //public string AvatarImg
        //{
        //    get
        //    {
        //        if (Avatar != null)
        //            return Avatar;
        //        if (ExtLogins == null)
        //            return "/img/default-avatar-male.webp";
        //        var fbLogin = ExtLogins.FirstOrDefault(x => x.LoginProvider == "Facebook");
        //        if (fbLogin == null)
        //            return "/img/default-avatar-male.webp";
        //        else
        //            return string.Format("https://graph.facebook.com/{0}/picture?type=large", fbLogin.ProviderKey);

        //    }
        //}

        //[NotMapped]
        //public string FacebookUrl
        //{
        //    get
        //    {
        //        if (ExtLogins == null)
        //            return null;

        //        var fbLogin = ExtLogins.FirstOrDefault(x => x.LoginProvider == "Facebook");
        //        if (fbLogin == null)
        //            return null;

        //        return string.Format("https://www.facebook.com/app_scoped_user_id/{0}", fbLogin.ProviderKey);
        //    }
        //}
    }

}
