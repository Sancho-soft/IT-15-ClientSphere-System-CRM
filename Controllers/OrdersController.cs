using ClientSphere.Models;
using ClientSphere.Services;
using ClientSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(IOrderService orderService, ICustomerService customerService, IProductService productService, UserManager<ApplicationUser> userManager)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
            _userManager = userManager;
        }

        // GET: Orders
        public async Task<IActionResult> Index(bool archived = false)
        {
            var allOrders = await _orderService.GetAllOrdersAsync();
            var orders = archived ? allOrders.Where(o => o.Status == Models.OrderStatus.Completed || o.Status == Models.OrderStatus.Cancelled) : allOrders.Where(o => o.Status != Models.OrderStatus.Completed && o.Status != Models.OrderStatus.Cancelled);
            ViewData["IsArchived"] = archived;
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
            // Auto-sync Customer role users into Customers table
            var customerUsers = await _userManager.GetUsersInRoleAsync("Customer");
            var existingCustomers = await _customerService.GetAllCustomersAsync();
            foreach (var user in customerUsers)
            {
                if (!existingCustomers.Any(c => c.Email == user.Email))
                {
                    await _customerService.CreateCustomerAsync(new Customer
                    {
                        CompanyName = $"{user.FirstName} {user.LastName}",
                        ContactName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email ?? "",
                        Phone = user.PhoneNumber ?? "",
                        IsActive = true
                    });
                }
            }

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
                    Text = $"{p.Name} - ₱{p.Price}"
                }),
                Items = new List<OrderItemViewModel> { new OrderItemViewModel() }
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

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
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

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order)
        {
             if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // For now, we only update status and date, not items
                     var existingOrder = await _orderService.GetOrderByIdAsync(id);
                     if (existingOrder != null)
                     {
                         existingOrder.Status = order.Status;
                         existingOrder.OrderDate = order.OrderDate;
                         // existingOrder.TotalAmount = order.TotalAmount; // Should be calculated
                         await _orderService.UpdateOrderAsync(existingOrder);
                     }
                }
                catch (Exception)
                {
                     if (await _orderService.GetOrderByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }
    }
}
