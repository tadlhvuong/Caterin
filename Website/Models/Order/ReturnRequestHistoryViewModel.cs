
namespace Website.Models.Order
{
    public class ReturnRequestHistoryViewModel
    {
        public string? StatusFilter { get; set; }

        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ProcessingRequests { get; set; }
        public int CompletedRequests { get; set; }

        public List<ReturnRequestHistoryItemViewModel> Requests { get; set; } = [];
    }

    public class ReturnRequestHistoryItemViewModel
    {
        public string RequestCode { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;

        public string RequestType { get; set; } = string.Empty;
        public string RequestTypeLabel { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public string ReasonLabel { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "warning";

        public DateTime RequestedAt { get; set; }

        public int TotalItems { get; set; }
        public decimal TotalReturnAmount { get; set; }

        public string? FirstProductName { get; set; }
        public string? FirstProductImage { get; set; }
    }
}
