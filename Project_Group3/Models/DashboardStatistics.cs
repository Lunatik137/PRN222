using System.Collections.Generic;

namespace PRN222_Group3.Models
{
    public class DashboardStatistics
    {
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }

        // Chuỗi thời gian cho doanh thu, đơn hàng, người dùng mới
        public List<TimeSeriesPoint> RevenueData { get; set; } = new();
        public List<TimeSeriesPoint> OrdersData { get; set; } = new();
        public List<TimeSeriesPoint> NewUsersData { get; set; } = new();

        // Top statistics
        public List<TopSellerStat> TopSellers { get; set; } = new();
        public List<TopProductStat> TopProducts { get; set; } = new();
        public List<TopBuyerStat> TopBuyers { get; set; } = new();
    }

    public class TimeSeriesPoint
    {
        public int Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public int? Quarter { get; set; }
        public decimal Value { get; set; }
    }

    public class TopSellerStat
    {
        public int SellerId { get; set; }
        public string SellerName { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopProductStat
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopBuyerStat
    {
        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }
}