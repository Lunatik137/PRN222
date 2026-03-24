namespace Project_Group3.Services;

public interface IProductReportTracker
{
    void AddReport(int productId, string reason, string reporter, DateTime reportedAtUtc);

    int GetReportCount(int productId);

    IReadOnlyList<string> GetReasons(int productId);
}
