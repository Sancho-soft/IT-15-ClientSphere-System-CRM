using ClientSphere.Models;
using ClientSphere.Services;
using ClientSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;

        public OrdersController(IOrderService orderService, ICustomerService customerService, IProductService productService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _orderService.GetOrderByIdAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            var products = await _productService.GetAllProductsAsync();

            var viewModel = new OrderCreateViewModel
            {
                CustomerList = customers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.CompanyName} ({c.ContactName})"
                }),
                ProductList = products.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} - ${p.Price}"
                }),
                Items = new List<OrderItemViewModel> { new OrderItemViewModel() } // Start with one empty row
            };

            return View(viewModel);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var order = new Order
                {
                    CustomerId = viewModel.CustomerId,
                    OrderItems = viewModel.Items
                        .Where(i => i.ProductId != 0 && i.Quantity > 0)
                        .Select(i => new OrderItem
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity
                        }).ToList()
                };

                if (order.OrderItems.Any())
                {
                    await _orderService.CreateOrderAsync(order);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Please add at least one product to the order.");
                }
            }

            // Reload lists if invalid
            var customers = await _customerService.GetAllCustomersAsync();
            var products = await _productService.GetAllProductsAsync();
            viewModel.CustomerList = customers.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.CompanyName} ({c.ContactName})" });
            viewModel.ProductList = products.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.Name} - ${p.Price}" });

            return View(viewModel);
        }
    }
}
