namespace Website.Models.Order;

public class ReturnRequestDetailsViewModel
{
    public string RequestCode { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;
    public string RequestTypeLabel { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
    public string ReasonLabel { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string Status { get; set; } = "Pending";
    public string StatusLabel { get; set; } = "Chờ xác nhận";
    public string StatusColor { get; set; } = "warning";

    public DateTime RequestedAt { get; set; }

    public List<ReturnRequestDetailsItemViewModel> Items { get; set; } = [];

    public decimal TotalReturnAmount =>
        Items.Sum(x => x.UnitPrice * x.ReturnQuantity);
}

public class ReturnRequestDetailsItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }
    public int PurchasedQuantity { get; set; }
    public int ReturnQuantity { get; set; }

    public decimal TotalPrice => UnitPrice * ReturnQuantity;
}