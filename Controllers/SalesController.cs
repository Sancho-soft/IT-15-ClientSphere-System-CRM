using ClientSphere.Models;
using ClientSphere.Services;
using ClientSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly IOrderService _orderService;

        public SalesController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllOrdersAsync();

            // Calculate stats
            // Note: In a real app, these should be done in the DB query for performance
            var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
            var pendingOrders = orders.Where(o => o.Status == OrderStatus.Pending).ToList();

            var viewModel = new SalesDashboardViewModel
            {
                TotalRevenue = completedOrders.Sum(o => o.TotalAmount),
                PendingRevenue = pendingOrders.Sum(o => o.TotalAmount),
                TotalSalesCount = completedOrders.Count,
                AvgDealSize = completedOrders.Any() ? completedOrders.Average(o => o.TotalAmount) : 0,
                RecentSales = orders.OrderByDescending(o => o.OrderDate).Take(20).ToList()
            };

            return View(viewModel);
        }
    }
}
