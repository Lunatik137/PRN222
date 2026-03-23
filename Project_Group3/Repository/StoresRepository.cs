

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Repository
{
    public class StoresRepository
    {
        private readonly CloneEbayDbContext _context;

        public StoresRepository(CloneEbayDbContext context)
        {
            _context = new();
        }
        public async Task<(List<object>, int)> GetPagedStoresAsync(string searchString, int page, int pageSize)
        {
            var query = _context.Stores
                .Include(s => s.Seller)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchString))
            {

                query = query.Where(s =>
                s.StoreName.Contains(searchString) ||
            (s.Seller != null && s.Seller.Username.Contains(searchString))
        );
            }

            var total = await query.CountAsync();
            if (total == 0)
            {
                return (new List<object>(), 0);
            }

            // Phần còn lại của code (OrderBy, Skip, Take, Select...) giữ nguyên
            var data = await query
                .OrderBy(s => s.StoreName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.StoreName,
                    SellerName = (s.Seller != null) ? s.Seller.Username : "N/A (Seller Not Found)",
                    OrderStats = _context.Products
                        .Where(p => p.SellerId == s.SellerId)
                        .SelectMany(p => p.OrderItems)
                        .Where(oi => oi.Order != null)
                        .Select(oi => new { oi.OrderId, oi.Order.Status })
                        .Distinct()
                        .GroupBy(g => g.Status)
                        .Select(g => new
                        {
                            Status = g.Key,
                            Count = g.Count()
                        })
                        .ToList()
                })
                .ToListAsync();

            // Phần map kết quả cuối cùng không đổi
            var finalData = data.Select(s =>
            {
                var stats = s.OrderStats;
                return new
                {
                    s.Id,
                    s.StoreName,
                    s.SellerName,
                    TotalOrders = stats.Sum(st => st.Count),
                    Processing = stats.FirstOrDefault(st => st.Status == "Processing")?.Count ?? 0,
                    Shipped = stats.FirstOrDefault(st => st.Status == "Shipped")?.Count ?? 0,
                    Delivered = stats.FirstOrDefault(st => st.Status == "Delivered")?.Count ?? 0,
                    Completed = stats.FirstOrDefault(st => st.Status == "Completed")?.Count ?? 0,
                    RequestCancel = stats.FirstOrDefault(st => st.Status == "RequestCancel")?.Count ?? 0,
                    Cancelled = stats.FirstOrDefault(st => st.Status == "Cancelled")?.Count ?? 0,
                    Returned = stats.FirstOrDefault(st => st.Status == "Returned")?.Count ?? 0
                };
            })
            .Cast<object>()
            .ToList();

            return (finalData, total);
        }

        public async Task<dynamic> GetStoreDetailsWithOrdersAsync(int storeId, string statusFilter, int page, int pageSize)
        {
            // Lấy thông tin Store (như cũ)
            var store = await _context.Stores
                .Include(s => s.Seller)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (store == null || store.SellerId == 0)
            {
                return null;
            }

            // 2. Xây dựng truy vấn cho đơn hàng
            var ordersQuery = _context.OrderItems
                .Where(oi => oi.Product.SellerId == store.SellerId)
                .Select(oi => oi.Order)
                .Distinct();

            // 3. Áp dụng Filter (Lọc)
            if (!string.IsNullOrEmpty(statusFilter))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == statusFilter);
            }

            // 4. Lấy tổng số lượng (sau khi lọc) để phân trang
            var totalOrders = await ordersQuery.CountAsync();

            // 5. Áp dụng Phân trang (Pagination)
            var pagedOrders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.Buyer) // Cần Include Buyer để lấy tên
                .Select(o => new // Tạo đối tượng dynamic cho mỗi đơn hàng
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    BuyerName = (o.Buyer != null) ? o.Buyer.Username : "N/A",
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    ReturnRequestDate = _context.ReturnRequests
            .Where(r => r.OrderId == o.Id)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefault()
                })
                .ToListAsync();

            // 6. Tạo ViewModel (dynamic) để trả về View
            dynamic viewModel = new ExpandoObject();
            viewModel.StoreId = store.Id;
            viewModel.StoreName = store.StoreName;
            viewModel.SellerName = (store.Seller != null) ? store.Seller.Username : "N/A";
            viewModel.Orders = pagedOrders; // Danh sách đơn hàng đã lọc và phân trang

            // Thêm thông tin phân trang
            viewModel.TotalOrders = totalOrders;
            viewModel.Page = page;
            viewModel.PageSize = pageSize;
            viewModel.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            return viewModel;
        }


        public async Task<ReturnRequest> GetReturnRequestByOrderIdAsync(int orderId)
        {
            // Tìm bản ghi ReturnRequest dựa trên OrderId
            return await _context.ReturnRequests
                .Include(rr => rr.User)
                .Include(rr => rr.Order)
                .AsNoTracking()
                .FirstOrDefaultAsync(rr => rr.OrderId == orderId);
        }

        // 2. Hàm mới để cập nhật trạng thái Order
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.OrderTables.FindAsync(orderId);
            if (order == null)
            {
                return false;
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> UpdateReturnRequestStatusByOrderIdAsync(int orderId, string newStatus)
        {
            // Tìm ReturnRequest dựa trên OrderId
            var returnRequest = await _context.ReturnRequests
                                             .FirstOrDefaultAsync(rr => rr.OrderId == orderId);

            if (returnRequest == null)
            {
                // Không tìm thấy ReturnRequest tương ứng (có thể không phải lỗi)
                return false;
            }

            returnRequest.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<OrderTable> GetOrderByIdAsync(int orderId)
        {
            // Giả sử repository của bạn có biến _context (là DbContext)
            return await _context.OrderTables.FindAsync(orderId);
        }

        // Hàm này bạn đã có (dùng cho dòng đầu tiên)

        // find overdate return requests

        public async Task<List<ReturnRequest>> FindOverDateRequest()
        {
            return await _context.ReturnRequests
                .Include(rr => rr.User)
                .Include(rr => rr.Order)
                .Where(rr => rr.Status == "Pending" &&
                             rr.CreatedAt.HasValue &&
                             EF.Functions.DateDiffDay(rr.CreatedAt.Value, DateTime.UtcNow) > 2)
                .ToListAsync();
        }



    }
}
