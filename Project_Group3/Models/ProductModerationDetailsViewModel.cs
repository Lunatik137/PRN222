namespace Project_Group3.Models;

public sealed class ProductModerationDetailsViewModel
{
    public required Product Product { get; init; }

    public int ReportCount { get; init; }

    public IReadOnlyList<string> ReportReasons { get; init; } = [];

    public string ReturnStatus { get; init; } = "reported";

    public string? ReturnKeyword { get; init; }
}
