using System.ComponentModel.DataAnnotations;

namespace Project_Group3.Models;

public sealed class ProductModerationViewModel
{
    public required IReadOnlyList<ProductModerationItemViewModel> Products { get; init; }
    public required IReadOnlyList<AdminActionLogItem> ActionLogs { get; init; }

    public string Status { get; init; } = "reported";

    public string? Keyword { get; init; }

    public int Total { get; init; }
}

public sealed class ProductModerationItemViewModel
{
    public required Product Product { get; init; }

    public int ReportCount { get; init; }
}

public sealed class ProductModerationFilterInput
{
    public string? Status { get; init; } = "reported";

    public string? Keyword { get; init; }
}

public sealed class ModerateProductInput
{
    [Required]
    public int ProductId { get; init; }

    [Required]
    [StringLength(250)]
    public string Reason { get; init; } = string.Empty;

    public string? Status { get; init; } = "reported";

    public string? Keyword { get; init; }

    public bool LockSeller { get; init; }
}

public sealed class ReportProductInput
{
    [Required]
    public int ProductId { get; init; }

    [Required]
    [StringLength(250)]
    public string Reason { get; init; } = string.Empty;

    public string? Status { get; init; } = "reported";

    public string? Keyword { get; init; }
}