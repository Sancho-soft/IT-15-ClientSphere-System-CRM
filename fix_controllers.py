import re

# Fix Support Controller
with open("d:/VS REPO/ClientSphere/Controllers/SupportController.cs", "r", encoding="utf-8") as f:
    supp = f.read()

supp = supp.replace('public async Task<IActionResult> Index()', 'public async Task<IActionResult> Index(bool archived = false)')
supp = supp.replace('var tickets = await _supportService.GetAllTicketsAsync();', '''var allTickets = await _supportService.GetAllTicketsAsync();
            var tickets = archived ? allTickets.Where(t => t.Status == "Closed" || t.Status == "Resolved") : allTickets.Where(t => t.Status != "Closed" && t.Status != "Resolved");
            ViewData["IsArchived"] = archived;''')

with open("d:/VS REPO/ClientSphere/Controllers/SupportController.cs", "w", encoding="utf-8") as f:
    f.write(supp)

# Fix Sales (Orders) Controller
with open("d:/VS REPO/ClientSphere/Controllers/OrdersController.cs", "r", encoding="utf-8") as f:
    orders = f.read()

orders = orders.replace('public async Task<IActionResult> Index()', 'public async Task<IActionResult> Index(bool archived = false)')
orders = orders.replace('var orders = await _orderService.GetAllOrdersAsync();', '''var allOrders = await _orderService.GetAllOrdersAsync();
            var orders = archived ? allOrders.Where(o => o.Status == "Closed" || o.Status == "Cancelled") : allOrders.Where(o => o.Status != "Closed" && o.Status != "Cancelled");
            ViewData["IsArchived"] = archived;''')

with open("d:/VS REPO/ClientSphere/Controllers/OrdersController.cs", "w", encoding="utf-8") as f:
    f.write(orders)

# Fix Marketing Controller
with open("d:/VS REPO/ClientSphere/Controllers/MarketingController.cs", "r", encoding="utf-8") as f:
    mktg = f.read()

mktg = mktg.replace('public async Task<IActionResult> Index()', 'public async Task<IActionResult> Index(bool archived = false)')
mktg = mktg.replace('var campaigns = await _campaignService.GetAllCampaignsAsync();', '''var allCampaigns = await _campaignService.GetAllCampaignsAsync();
            var campaigns = archived ? allCampaigns.Where(c => c.Status == "Completed" || c.Status == "Cancelled") : allCampaigns.Where(c => c.Status != "Completed" && c.Status != "Cancelled");
            // Also need to fix the sum logic to not break if list is empty, but we convert to List first
            campaigns = campaigns.ToList();
            ViewData["IsArchived"] = archived;''')
with open("d:/VS REPO/ClientSphere/Controllers/MarketingController.cs", "w", encoding="utf-8") as f:
    f.write(mktg)

# Fix Customers View because it passes archived parameter but the controller uses IsActive!
with open("d:/VS REPO/ClientSphere/Views/Customers/Index.cshtml", "r", encoding="utf-8") as f:
    cust_view = f.read()

cust_view = cust_view.replace('bool isArchived = Context.Request.Query["archived"] == "1";', 'bool isArchived = Context.Request.Query["archived"] == "1";')
# wait actually CustomersController filters based on IsActive if we inject it there
with open("d:/VS REPO/ClientSphere/Controllers/CustomersController.cs", "r", encoding="utf-8") as f:
    cust = f.read()

cust = cust.replace('public async Task<IActionResult> Index(string searchString)', 'public async Task<IActionResult> Index(string searchString, bool archived = false)')
cust = cust.replace('customers = await _customerService.SearchCustomersAsync(searchString);', 'customers = await _customerService.SearchCustomersAsync(searchString);\n                customers = customers.Where(c => archived ? !c.IsActive : c.IsActive);')
cust = cust.replace('customers = await _customerService.GetAllCustomersAsync();', 'customers = await _customerService.GetAllCustomersAsync();\n                customers = customers.Where(c => archived ? !c.IsActive : c.IsActive);')

with open("d:/VS REPO/ClientSphere/Controllers/CustomersController.cs", "w", encoding="utf-8") as f:
    f.write(cust)

print("Done")
