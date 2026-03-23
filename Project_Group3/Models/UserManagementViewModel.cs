using System.ComponentModel.DataAnnotations;

namespace Project_Group3.Models;

public sealed class UserManagementViewModel
{
    public required IReadOnlyList<User> Users { get; init; }

    public required IReadOnlyList<AdminActionLogItem> ActionLogs { get; init; }

    public string? Keyword { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int Total { get; init; }

    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed class UserManagementFilterInput
{
    public string? Keyword { get; init; }

    public string? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(5, 100)]
    public int PageSize { get; init; } = 10;
}

public sealed class LockUserInput
{
    [Required]
    public int Id { get; init; }

    [Required]
    [StringLength(250)]
    public string Reason { get; init; } = string.Empty;

    public string? Keyword { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class AdminActionLogItem
{
    public required DateTime AtUtc { get; init; }

    public required string Action { get; init; }

    public required string Username { get; init; }

    public required string Target { get; init; }

    public required string Details { get; init; }
}
