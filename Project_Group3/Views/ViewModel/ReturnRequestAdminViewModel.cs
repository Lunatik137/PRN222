using PRN222_Group3.Helper;

namespace PRN222_Group3.Views.ViewModel;

public class ReturnRequestAdminListItemViewModel
{
    public int ReturnRequestId { get; set; }
    public int? OrderId { get; set; }
    public string? BuyerUsername { get; set; }
    public string? OrderStatus { get; set; }
    public string? ReturnStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? ReasonPreview { get; set; }
}

public class ReturnRequestAdminDetailsViewModel
{
    public int ReturnRequestId { get; set; }
    public int? OrderId { get; set; }
    public int? BuyerId { get; set; }
    public string? BuyerUsername { get; set; }
    public string? BuyerEmail { get; set; }
    public string? OrderStatus { get; set; }
    public decimal? OrderTotal { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? ReturnStatus { get; set; }
    public string? Reason { get; set; }
    public DateTime? CreatedAt { get; set; }
    public IReadOnlyList<string> ImageFileNames { get; set; } = Array.Empty<string>();
    public bool CanProcess { get; set; }
}

public class ReturnRequestAdminIndexPageViewModel
{
    public PaginatedResult<ReturnRequestAdminListItemViewModel> Result { get; set; } = new();
    public string? StatusFilter { get; set; }
}
