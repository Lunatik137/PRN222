using Project_Group3.Models;

namespace Project_Group3.ViewModel;

public class ReturnRequestAdminListItemViewModel
{
    public int ReturnRequestId { get; set; }
    public int? OrderId { get; set; }
    public string? BuyerUsername { get; set; }
    public string? BuyerEmail { get; set; }
    public string? OrderStatus { get; set; }
    public string? ReturnStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? ReasonPreview { get; set; }
}

public class ReturnRequestAdminDetailsViewModel
{
    public int ReturnRequestId { get; set; }
    public int? OrderId { get; set; }
    public string? BuyerUsername { get; set; }
    public string? BuyerEmail { get; set; }
    public string? OrderStatus { get; set; }
    public decimal? OrderTotal { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? ReturnStatus { get; set; }
    public string? Reason { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool CanProcess { get; set; }
    public List<string> Images { get; set; } = [];
}

public class ReturnRequestAdminIndexPageViewModel
{
    public List<ReturnRequestAdminListItemViewModel> Items { get; set; } = [];
    public string? Status { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
