import re

# Fix Support
with open("d:/VS REPO/ClientSphere/Controllers/SupportController.cs", "r", encoding="utf-8") as f:
    supp = f.read()
supp = supp.replace('TotalTickets = tickets.Count,', 'TotalTickets = tickets.Count(),')
with open("d:/VS REPO/ClientSphere/Controllers/SupportController.cs", "w", encoding="utf-8") as f:
    f.write(supp)

# Fix Orders
with open("d:/VS REPO/ClientSphere/Controllers/OrdersController.cs", "r", encoding="utf-8") as f:
    orders = f.read()
orders = orders.replace('TotalOrders = orders.Count,', 'TotalOrders = orders.Count(),')
orders = orders.replace('var totalRevenue = orders.Sum(o => o.TotalAmount);', 'var totalRevenue = orders.Any() ? orders.Sum(o => o.TotalAmount) : 0;')
with open("d:/VS REPO/ClientSphere/Controllers/OrdersController.cs", "w", encoding="utf-8") as f:
    f.write(orders)

# Fix Marketing
with open("d:/VS REPO/ClientSphere/Controllers/MarketingController.cs", "r", encoding="utf-8") as f:
    mktg = f.read()
mktg = mktg.replace('TotalCampaigns = campaigns.Count,', 'TotalCampaigns = campaigns.Count(),')
mktg = mktg.replace('TotalRecipients = campaigns.Sum(c => c.TargetAudienceSize),', 'TotalRecipients = campaigns.Any() ? campaigns.Sum(c => c.TargetAudienceSize) : 0,')
mktg = mktg.replace('ActiveBudget = campaigns.Where(c => c.Status == "Active").Sum(c => c.Budget),', 'ActiveBudget = campaigns.Any(c => c.Status == "Active") ? campaigns.Where(c => c.Status == "Active").Sum(c => c.Budget) : 0,')
with open("d:/VS REPO/ClientSphere/Controllers/MarketingController.cs", "w", encoding="utf-8") as f:
    f.write(mktg)

print("Done")
