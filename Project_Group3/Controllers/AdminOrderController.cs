using Microsoft.AspNetCore.Mvc;

public class AdminOrderController : Controller
{
    private readonly IOrderRepository _orderRepo;

    public AdminOrderController(IOrderRepository orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public IActionResult Index()
    {
        var orders = _orderRepo.GetAllOrders();
        return View(orders);
    }

    public IActionResult Details(int id)
    {
        var order = _orderRepo.GetOrderById(id);
        var items = _orderRepo.GetOrderItems(id);

        ViewBag.Items = items;

        return View(order);
    }

    [HttpPost]
    public IActionResult UpdateStatus(int id, string status)
    {
        _orderRepo.UpdateOrderStatus(id, status);

        return RedirectToAction("Details", new { id = id });
    }
}