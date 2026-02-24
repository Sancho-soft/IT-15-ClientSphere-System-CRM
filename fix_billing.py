import sys

with open('d:/VS REPO/ClientSphere/Views/Billing/Create.cshtml', 'r') as f:
    c = f.read()

old = '''                     <div class="col-md-6">
                        <label asp-for="CustomerId" class="form-label fw-bold">Customer ID</label>
                        <input asp-for="CustomerId" class="form-control" placeholder="Enter Customer ID" type="number" />
                        <span asp-validation-for="CustomerId" class="text-danger small"></span>
                    </div>'''

new = '''                     <div class="col-md-3">
                        <label asp-for="CustomerId" class="form-label fw-bold">Select Customer</label>
                        <select asp-for="CustomerId" class="form-select" asp-items="ViewBag.Customers">
                            <option value="">-- Choose Customer --</option>
                        </select>
                        <span asp-validation-for="CustomerId" class="text-danger small"></span>
                    </div>
                     <div class="col-md-3">
                        <label asp-for="OrderId" class="form-label fw-bold">Link to Order (Optional)</label>
                        <select asp-for="OrderId" class="form-select" asp-items="ViewBag.Orders">
                            <option value="0">-- None --</option>
                        </select>
                        <span asp-validation-for="OrderId" class="text-danger small"></span>
                    </div>'''

c = c.replace(old, new)

with open('d:/VS REPO/ClientSphere/Views/Billing/Create.cshtml', 'w') as f:
    f.write(c)

