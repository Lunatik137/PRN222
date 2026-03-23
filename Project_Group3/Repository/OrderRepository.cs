using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;

public class OrderRepository : IOrderRepository
{
    private readonly CloneEbayDbContext _context;

    public OrderRepository(CloneEbayDbContext context)
    {
        _context = context;
    }


    public List<OrderTable> GetAllOrders()
    {
        return _context.OrderTables
            .Include(o => o.buyer)
            .OrderByDescending(o => o.orderDate)
            .ToList();
    }

    public OrderTable GetOrderById(int id)
    {
        return _context.OrderTables
            .FirstOrDefault(o => o.id == id);
    }

    public List<OrderItem> GetOrderItems(int orderId)
    {
        return _context.OrderItems
            .Where(o => o.orderId == orderId)
            .Include(o => o.product)
            .ToList();
    }

    public void UpdateOrderStatus(int id, string status)
    {
        var order = _context.OrderTables.FirstOrDefault(o => o.id == id);

        if (order != null)
        {
            order.status = status;
            _context.SaveChanges();
        }
    }
}