
using Project_Group3.Models;

public interface IOrderRepository
{
    List<OrderTable> GetAllOrders();

    OrderTable GetOrderById(int id);

    List<OrderItem> GetOrderItems(int orderId);

    void UpdateOrderStatus(int id, string status);
}
