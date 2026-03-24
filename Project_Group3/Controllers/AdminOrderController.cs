using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Project_Group3.Security;

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
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = _orderRepo.GetAllOrders();
        return View(orders);
    }

    public IActionResult Details(int id)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var order = _orderRepo.GetOrderById(id);
        var items = _orderRepo.GetOrderItems(id);

        ViewBag.Items = items;

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        if (!CanProcessOrders())
        {
            return RedirectToAction("Login", "Account");
        }

        _orderRepo.UpdateOrderStatus(id, status);

        var adminName = HttpContext.Session.GetString("Username") ?? "Admin";

        await _hub.Clients.All.SendAsync("OrderUpdated",
            $"{adminName} updated Order #{id} ? {status}");

        return RedirectToAction("Details", new { id });
    }

    private bool HasAdminAccess()
        => HttpContext.HasAdminPermission(AdminPermissions.CanAccessOrders);

    private bool CanProcessOrders()
        => HttpContext.HasAdminPermission(AdminPermissions.CanProcessOrders);
}

