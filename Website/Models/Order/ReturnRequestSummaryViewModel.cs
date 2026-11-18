namespace Website.Models.Order;

public class ReturnRequestSummaryViewModel
{
    public string RequestCode { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;
    public string RequestTypeLabel { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string Status { get; set; } = "Pending";
    public string StatusLabel { get; set; } = "Chờ xác nhận";
    public string StatusColor { get; set; } = "warning";

    public DateTime RequestedAt { get; set; }
}