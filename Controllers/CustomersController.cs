using ClientSphere.Models;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize]
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

            // Auto-sync Customer role users into the Customers table
            var customerUsers = await _userManager.GetUsersInRoleAsync("Customer");
            var existingCustomers = await _customerService.GetAllCustomersAsync();
            foreach (var user in customerUsers)
            {
                // Check if this user already has a Customer record (by email)
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

            IEnumerable<Customer> customers;
            if (!string.IsNullOrEmpty(searchString))
            {
                customers = await _customerService.SearchCustomersAsync(searchString);
                customers = customers.Where(c => archived ? !c.IsActive : c.IsActive);
            }
            else
            {
                customers = await _customerService.GetAllCustomersAsync();
                customers = customers.Where(c => archived ? !c.IsActive : c.IsActive);
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
                catch (Exception) // Catching generic exception for now, ideally specific concurrency exception
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

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _customerService.DeleteCustomerAsync(id);
            return RedirectToAction(nameof(Index));
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
