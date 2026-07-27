using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email.EmailModels
{
    public class MarketingEmailModel : BaseEmailModel
    {
        // Người nhận
        public string CustomerName { get; set; }


        // Nội dung chiến dịch
        public string CampaignName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }


        // Hình ảnh banner
        public string BannerUrl { get; set; }


        // CTA
        public string ActionUrl { get; set; }

        public string ActionText { get; set; }


        // Sản phẩm nổi bật
        public List<MarketingProductItem> Products { get; set; }


        // Voucher (nếu có)
        public string VoucherCode { get; set; }

        public string VoucherValue { get; set; }

        public DateTime? ExpireDate { get; set; }

        //promotion
        //public string PromotionPeriod { get; set; }

        // Flash Sale
        //public string SaleStartTime { get; set; }
        //public string SaleEndTime { get; set; }
        //public int RemainingQuantity { get; set; }

        //campain
        //public string CampaignContent { get; set; }

        //public string CampaignImageUrl { get; set; }

        //public string SecondaryActionUrl { get; set; }

        //public string SecondaryActionText { get; set; }

        //holiday
        //public string HolidayName { get; set; }

        //public string GreetingMessage { get; set; }

        //public string GiftMessage { get; set; }

        //new letter
        //public string NewsletterTitle { get; set; }

        //public string NewsletterContent { get; set; }

        //public List<NewsletterArticleItem> Articles { get; set; }
    }
}
