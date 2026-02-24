path = "d:/VS REPO/ClientSphere/Views/Billing/Index.cshtml"
with open(path, encoding="utf-8") as f:
    c = f.read()

# 1) Add the isArchived variable and tab nav after the @{ block
old_div = '<div class="container-fluid p-0">\n    <!-- Header -->'
new_div = '''@{
    var isBillingArchived = Context.Request.Query["archived"].ToString() == "1";
}

<div class="container-fluid p-0">
    <!-- Header -->'''
c = c.replace(old_div, new_div)

# 2) Add the tab nav after the closing </div> of the header
old_header_end = '    </div>\n\n    <!-- Stats Cards -->'
new_header_end = '''    </div>

    <!-- Archive Tabs (Staff/Admin only) -->
    @if (User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Billing Staff"))
    {
        <ul class="nav nav-tabs mb-4">
            <li class="nav-item">
                <a class="nav-link @(!isBillingArchived ? "active fw-bold" : "")" href="?archived=0">
                    <i class="bi bi-receipt me-1"></i> Active Invoices
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link @(isBillingArchived ? "active fw-bold" : "")" href="?archived=1">
                    <i class="bi bi-archive me-1"></i> Archived
                </a>
            </li>
        </ul>
    }

    <!-- Stats Cards -->'''
c = c.replace(old_header_end, new_header_end)

# 3) Add archive button after mark paid block
old_btn = '''                                        }
                                    </div>
                                </td>
                            </tr>'''
new_btn = '''                                        }
                                        @if (User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Billing Staff"))
                                        {
                                            @if (!isBillingArchived)
                                            {
                                                <form asp-action="ArchiveInvoice" asp-route-id="@invoice.Id" method="post" class="d-inline">
                                                    @Html.AntiForgeryToken()
                                                    <button type="submit" class="btn btn-sm btn-outline-secondary rounded-pill px-2 py-0" style="font-size:0.75rem;" title="Archive">
                                                        <i class="bi bi-archive"></i>
                                                    </button>
                                                </form>
                                            }
                                            else
                                            {
                                                <form asp-action="UnarchiveInvoice" asp-route-id="@invoice.Id" method="post" class="d-inline">
                                                    @Html.AntiForgeryToken()
                                                    <button type="submit" class="btn btn-sm btn-outline-success rounded-pill px-2 py-0" style="font-size:0.75rem;" title="Restore">
                                                        <i class="bi bi-arrow-counterclockwise"></i> Restore
                                                    </button>
                                                </form>
                                            }
                                        }
                                    </div>
                                </td>
                            </tr>'''
c = c.replace(old_btn, new_btn)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
print("Done")
