using PRN222_Group3.Models;

namespace PRN222_Group3.Views.ViewModel
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalRevenue { get; set; }
        public List<Product> RecentProducts { get; set; }
        public List<OrderTable> RecentOrders { get; set; }
    }
}
