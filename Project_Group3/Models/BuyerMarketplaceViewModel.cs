using System.ComponentModel.DataAnnotations;

namespace Project_Group3.Models;

public sealed class BuyerMarketplaceViewModel
{
    public bool IsBuyer { get; init; }
    public IReadOnlyList<BuyerProductCardViewModel> Products { get; init; } = [];
}

public sealed class BuyerProductCardViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string? ImageUrl { get; init; }
    public string? CategoryName { get; init; }
    public string? SellerName { get; init; }
}

public sealed class BuyerReportProductInput
{
    public int ProductId { get; init; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; init; } = string.Empty;
}
