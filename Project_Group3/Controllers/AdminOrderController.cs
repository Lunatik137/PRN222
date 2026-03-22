using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

public class AdminOrderController : Controller
{
    private readonly IOrderRepository _orderRepo;
    private readonly IHubContext<NotificationHub> _hub;

    public AdminOrderController(IOrderRepository repo,
                                IHubContext<NotificationHub> hub)
    {
        _orderRepo = repo;
        _hub = hub;
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
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        _orderRepo.UpdateOrderStatus(id, status);

        // lấy tên admin đang login
        var adminName = HttpContext.Session.GetString("Username") ?? "Admin";

        // gửi realtime
        await _hub.Clients.All.SendAsync("OrderUpdated",
            $"{adminName} updated Order #{id} → {status}");

        return RedirectToAction("Details", new { id });
    }
}