using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;



namespace PRN222_Group3.Repository
{
    public class StatisticsRepository
    {
        private readonly CloneEbayDbContext _context;

        public StatisticsRepository()
        {
            _context = new CloneEbayDbContext();
        }

        public async Task<DashboardStatistics> GetDashboardStatistics(DateTime? startDate, DateTime? endDate, string groupBy, int page, int limit)
        {
            var queryOrders = _context.OrderTables.AsQueryable();

            // Normalize date range for top-statistics queries (controller always passes non-null values)
            var sDate = startDate ?? DateTime.MinValue;
            var eDate = endDate ?? DateTime.MaxValue;

            if (sDate != DateTime.MinValue && eDate != DateTime.MaxValue)
            {
                queryOrders = queryOrders.Where(o => o.OrderDate >= sDate && o.OrderDate <= eDate);
            }

            var revenueQuery = queryOrders.Where(o => o.Status == "Completed");

            var revenue = await revenueQuery
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            var orders = await queryOrders.CountAsync();

            // Chuẩn hóa groupBy
            groupBy = string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.ToLower();

            // Doanh thu theo chuỗi thời gian
            IQueryable<TimeSeriesPoint> revenueSeriesQuery;
            if (groupBy == "day")
            {
                revenueSeriesQuery = revenueQuery
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Value = g.Sum(o => (decimal?)o.TotalPrice) ?? 0m
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day);
            }
            else if (groupBy == "quarter")
            {
                revenueSeriesQuery = revenueQuery
                    .GroupBy(o => new { o.OrderDate.Year, Quarter = (o.OrderDate.Month - 1) / 3 + 1 })
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Quarter = g.Key.Quarter,
                        Value = g.Sum(o => (decimal?)o.TotalPrice) ?? 0m
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Quarter);
            }
            else
            {
                revenueSeriesQuery = revenueQuery
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Value = g.Sum(o => (decimal?)o.TotalPrice) ?? 0m
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month);
            }

            var revenueData = await revenueSeriesQuery.ToListAsync();

            // Người dùng mới theo chuỗi thời gian
            var usersQuery = _context.Users.AsQueryable();
            if (sDate != DateTime.MinValue && eDate != DateTime.MaxValue)
            {
                usersQuery = usersQuery.Where(u => u.CreatedAt >= sDate && u.CreatedAt <= eDate);
            }

            int newUsers = await usersQuery.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            IQueryable<TimeSeriesPoint> newUsersSeriesQuery;
            if (groupBy == "day")
            {
                newUsersSeriesQuery = usersQuery
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day);
            }
            else if (groupBy == "quarter")
            {
                newUsersSeriesQuery = usersQuery
                    .GroupBy(u => new { u.CreatedAt.Year, Quarter = (u.CreatedAt.Month - 1) / 3 + 1 })
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Quarter = g.Key.Quarter,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Quarter);
            }
            else
            {
                newUsersSeriesQuery = usersQuery
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month);
            }

            var newUsersData = await newUsersSeriesQuery.ToListAsync();

            // Đơn hàng theo chuỗi thời gian
            IQueryable<TimeSeriesPoint> ordersSeriesQuery;
            if (groupBy == "day")
            {
                ordersSeriesQuery = queryOrders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day);
            }
            else if (groupBy == "quarter")
            {
                ordersSeriesQuery = queryOrders
                    .GroupBy(o => new { o.OrderDate.Year, Quarter = (o.OrderDate.Month - 1) / 3 + 1 })
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Quarter = g.Key.Quarter,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Quarter);
            }
            else
            {
                ordersSeriesQuery = queryOrders
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new TimeSeriesPoint
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month);
            }

            var ordersData = await ordersSeriesQuery.ToListAsync();

            // Top sellers (by revenue)
            var topSellers = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Seller)
                .Where(oi => oi.Order != null
                             && oi.Order.OrderDate >= sDate
                             && oi.Order.OrderDate <= eDate)

                .GroupBy(oi => oi.Product.Seller)
                .Select(g => new TopSellerStat
                {
                    SellerId = g.Key.Id,
                    SellerName = g.Key.Username,
                    TotalOrders = g.Select(oi => oi.OrderId).Distinct().Count(),
                    TotalItems = g.Sum(oi => (int?)oi.Quantity) ?? 0,
                    TotalRevenue = g.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0m
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToListAsync();

            // Top products (by quantity sold)
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order != null
                             && oi.Order.OrderDate >= sDate
                             && oi.Order.OrderDate <= eDate)

                .GroupBy(oi => oi.Product)
                .Select(g => new TopProductStat
                {
                    ProductId = g.Key.Id,
                    ProductName = g.Key.Title,
                    TotalQuantity = g.Sum(oi => (int?)oi.Quantity) ?? 0,
                    TotalRevenue = g.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0m
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToListAsync();

            // Top buyers (by total spent)
            var topBuyers = await _context.OrderTables
                .Include(o => o.Buyer)
                .Where(o => o.Buyer != null
                            && o.OrderDate >= sDate
                            && o.OrderDate <= eDate
                            && o.Status == "Completed")
                .GroupBy(o => o.Buyer)
                .Select(g => new TopBuyerStat
                {
                    BuyerId = g.Key.Id,
                    BuyerName = g.Key.Username,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalPrice)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5)
                .ToListAsync();

            return new DashboardStatistics
            {
                Revenue = revenue,
                Orders = orders,
                TotalUsers = totalUsers,
                NewUsers = newUsers,
                RevenueData = revenueData,
                OrdersData = ordersData,
                NewUsersData = newUsersData,
                TopSellers = topSellers,
                TopProducts = topProducts,
                TopBuyers = topBuyers
            };
        }
    }
}