using Shared.Enums;
using Website.Areas.Admin.Models.Order;

public sealed class OrderListViewModel
{
    public IReadOnlyList<OrderListItemViewModel> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? Search { get; init; }

    public OrderStatus? Status { get; init; }
}