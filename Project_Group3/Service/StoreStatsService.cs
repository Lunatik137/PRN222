using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;
using System;
using System.Linq;

namespace PRN222_Group3.Service
{
    public class StoreStatsService
    {
        private readonly CloneEbayDbContext _db;

        public StoreStatsService(CloneEbayDbContext db)
        {
            _db = new();
        }

        public async Task<StoreStats> GetStatsAsync(int storeId, int days = 90)
        {
            // 1. Tìm store + sellerId
            var store = await _db.Stores.FindAsync(storeId);
            if (store == null)
            {
                throw new InvalidOperationException($"Store {storeId} không tồn tại.");
            }

            var sellerId = store.SellerId;

            // 2. Khoảng thời gian tính (VD 90 ngày gần nhất)
            var fromDate = DateTime.UtcNow.AddDays(-days);

            // 3. Lấy tất cả đơn đã hoàn tất của seller trong N ngày
            //    (join từ OrderItem -> Product -> sellerId)
            var sellerOrdersQuery =
                from o in _db.OrderTables           // hoặc _db.Orders tuỳ bạn map
                join oi in _db.OrderItems on o.Id equals oi.OrderId
                join p in _db.Products on oi.ProductId equals p.Id
                where p.SellerId == sellerId
                      // TODO: đổi cho đúng kiểu status của bạn
                      //&& o.Status == "Completed"    // hoặc OrderStatus.Completed
                      && o.OrderDate >= fromDate
                select o;

            // DISTINCT để tránh trùng đơn nếu nhiều item cùng seller
            var sellerOrders = await sellerOrdersQuery
                //.Distinct()
                .ToListAsync();

            if (!sellerOrders.Any())
            {
                // Không có đơn nào -> trả về all 0
                return new StoreStats();
            }

            var orderIds = sellerOrders.Select(o => o.Id).ToList();

            var totalOrders = sellerOrders.Count;
            var totalSales = sellerOrders.Sum(o => (decimal?)o.TotalPrice) ?? 0;
            

            // 4. Đơn bị trả hàng (ReturnRequest Approved)
            var returnedOrderIds = await _db.ReturnRequests
                .Where(rr => orderIds.Contains(rr.OrderId.Value)
                             && rr.Status == "Completed")
                .Select(rr => rr.OrderId)
                .Distinct()
                .ToListAsync();

            var returnCount = returnedOrderIds.Count;

            // 5. Đơn có Dispute và seller thua
            // TODO: đổi "SellerLost"/status cho đúng DB của bạn
            var disputedOrderIds = await _db.Disputes
                .Where(d => orderIds.Contains(d.OrderId.Value)
                            && d.Status == "Resolved")
                .Select(d => d.OrderId)
                .Distinct()
                .ToListAsync();

            var disputeCount = disputedOrderIds.Count;

            // 6. Đơn "defect" = đơn có return hoặc dispute
            var defectOrderIds = returnedOrderIds
                .Union(disputedOrderIds)
                .Distinct()
                .ToList();

            var defectCount = defectOrderIds.Count;

            // 7. Tính tỉ lệ (chia cho 0 thì trả về 0)
            decimal SafeRate(int count) =>
                totalOrders == 0 ? 0 : (decimal)count / totalOrders;

            var stats = new StoreStats
            {
                TotalOrders = totalOrders,
                TotalSales = totalSales,

                ReturnRate = SafeRate(returnCount),
                DisputeRate = SafeRate(disputeCount),
                DefectRate = SafeRate(defectCount)
            };

            return stats;
        }
    }

}
