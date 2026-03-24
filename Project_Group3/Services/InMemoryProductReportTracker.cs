using System.Collections.Concurrent;

namespace Project_Group3.Services;

public sealed class InMemoryProductReportTracker : IProductReportTracker
{
    private readonly ConcurrentDictionary<int, ConcurrentQueue<ProductReportRecord>> reportsByProduct = new();

    public void AddReport(int productId, string reason, string reporter, DateTime reportedAtUtc)
    {
        var reports = reportsByProduct.GetOrAdd(productId, _ => new ConcurrentQueue<ProductReportRecord>());
        reports.Enqueue(new ProductReportRecord(reason.Trim(), reporter.Trim(), reportedAtUtc));
    }

    public int GetReportCount(int productId)
        => reportsByProduct.TryGetValue(productId, out var reports) ? reports.Count : 0;

    public IReadOnlyList<string> GetReasons(int productId)
    {
        if (!reportsByProduct.TryGetValue(productId, out var reports) || reports.IsEmpty)
        {
            return [];
        }

        return reports
            .Select(report => $"[{report.ReportedAtUtc:yyyy-MM-dd HH:mm}] {report.Reporter}: {report.Reason}")
            .TakeLast(5)
            .Reverse()
            .ToList();
    }

    private sealed record ProductReportRecord(string Reason, string Reporter, DateTime ReportedAtUtc);
}