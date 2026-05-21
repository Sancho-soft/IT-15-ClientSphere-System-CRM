using ClientSphere.Models;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Super Admin,Admin,Sales Manager,Sales Staff,Support Staff,Marketing Manager,Marketing Staff,Billing Staff")]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
            _userManager = userManager;
        }

        // GET: Customers
        public async Task<IActionResult> Index(string searchString, bool archived = false)
        {
            ViewData["CurrentFilter"] = searchString;

            // Customer auto-sync removed from Index (Finding #5).
            // Sync is now handled at startup via DbInitializer.

            IEnumerable<Customer> customers;
            if (!string.IsNullOrEmpty(searchString))
            {
                customers = await _customerService.SearchCustomersAsync(searchString);
                // For search results, still need to filter by status in memory
                // since SearchAsync doesn't have a status parameter
                customers = customers.Where(c => archived ? !c.IsActive : c.IsActive);
            }
            else
            {
                // DB-level filtering by status (Finding #6)
                customers = await _customerService.GetCustomersByStatusAsync(!archived);
            }

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customerService.GetCustomerByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyName,ContactName,Email,Phone,Address,City,Region,PostalCode,Country")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.CreateCustomerAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customerService.GetCustomerByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyName,ContactName,Email,Phone,Address,City,Region,PostalCode,Country,CreatedAt")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _customerService.UpdateCustomerAsync(customer);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    if (!await _customerService.CustomerExistsAsync(customer.Id))
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
            return View(customer);
        }


        // POST: Customers/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            customer.IsActive = !customer.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;
            await _customerService.UpdateCustomerAsync(customer);

            TempData["ToastMessage"] = customer.IsActive
                ? $"{customer.CompanyName} has been set to Active."
                : $"{customer.CompanyName} has been set to Inactive.";
            TempData["ToastType"] = customer.IsActive ? "success" : "warning";

            return RedirectToAction(nameof(Index));
        }
    }
}
