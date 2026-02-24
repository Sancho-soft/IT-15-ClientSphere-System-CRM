# Support/Index.cshtml
path = "d:/VS REPO/ClientSphere/Views/Support/Index.cshtml"
with open(path, encoding="utf-8") as f:
    c = f.read()

c = c.replace(
    '@{\n    ViewData["Title"] = "Support Tickets";\n    Layout = "~/Views/Shared/_CrmLayout.cshtml";\n    ViewData["CurrentPage"] = "Support";\n    ViewData["HideTopNavTitle"] = true;\n}',
    '@{\n    ViewData["Title"] = "Support Tickets";\n    Layout = "~/Views/Shared/_CrmLayout.cshtml";\n    ViewData["CurrentPage"] = "Support";\n    ViewData["HideTopNavTitle"] = true;\n}\n\n@{\n    var isSupportArchived = Context.Request.Query["archived"].ToString() == "1";\n}'
)

old_hdr = '    </div>\n\n    <!-- Stats Cards -->'
new_hdr = '''    </div>

    <!-- Archive Tabs -->
    <ul class="nav nav-tabs mb-4">
        <li class="nav-item">
            <a class="nav-link @(!isSupportArchived ? "active fw-bold" : "")" href="?archived=0">
                <i class="bi bi-chat-square-text me-1"></i> Active Tickets
            </a>
        </li>
        <li class="nav-item">
            <a class="nav-link @(isSupportArchived ? "active fw-bold" : "")" href="?archived=1">
                <i class="bi bi-archive me-1"></i> Archived
            </a>
        </li>
    </ul>

    <!-- Stats Cards -->'''
c = c.replace(old_hdr, new_hdr, 1)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
print("Support done")

# Marketing/Index.cshtml
path = "d:/VS REPO/ClientSphere/Views/Marketing/Index.cshtml"
with open(path, encoding="utf-8") as f:
    c = f.read()

c = c.replace(
    '@{\n    ViewData["Title"] = "Marketing Campaigns";\n    Layout = "~/Views/Shared/_CrmLayout.cshtml";\n    ViewData["CurrentPage"] = "Marketing";\n    ViewData["HideTopNavTitle"] = true;\n}',
    '@{\n    ViewData["Title"] = "Marketing Campaigns";\n    Layout = "~/Views/Shared/_CrmLayout.cshtml";\n    ViewData["CurrentPage"] = "Marketing";\n    ViewData["HideTopNavTitle"] = true;\n}\n\n@{\n    var isCampaignArchived = Context.Request.Query["archived"].ToString() == "1";\n}'
)

old_btn = '    <!-- Create Campaign Button row -->\n    <div class="d-flex justify-content-end mb-4">'
new_btn = '''    <!-- Archive Tabs -->
    <ul class="nav nav-tabs mb-3">
        <li class="nav-item">
            <a class="nav-link @(!isCampaignArchived ? "active fw-bold" : "")" href="?archived=0">
                <i class="bi bi-megaphone me-1"></i> Active Campaigns
            </a>
        </li>
        <li class="nav-item">
            <a class="nav-link @(isCampaignArchived ? "active fw-bold" : "")" href="?archived=1">
                <i class="bi bi-archive me-1"></i> Archived
            </a>
        </li>
    </ul>

    <!-- Create Campaign Button row -->
    <div class="d-flex justify-content-end mb-4">'''
c = c.replace(old_btn, new_btn, 1)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
print("Marketing done")

# Sales/Index.cshtml
path = "d:/VS REPO/ClientSphere/Views/Sales/Index.cshtml"
with open(path, encoding="utf-8") as f:
    c = f.read()

c = c.replace(
    '@{\n    ViewData["Title"] = "Sales Management";\n    Layout = "~/Views/Shared/_CrmLayout.cshtml";\n    ViewData["CurrentPage"] = "Sales";\n    ViewData["HideTopNavTitle"] = true;\n}',
    '@{\n    ViewData["Title"] = "Sales Management";\n    Layout = "~/Views/Shared/_CrmLayout.cshtml";\n    ViewData["CurrentPage"] = "Sales";\n    ViewData["HideTopNavTitle"] = true;\n}\n\n@{\n    var isSalesArchived = Context.Request.Query["archived"].ToString() == "1";\n}'
)

old_hdr2 = '    </div>\n\n    <!-- Stats Cards -->\n    <div class="row mb-4">'
new_hdr2 = '''    </div>

    <!-- Archive Tabs -->
    <ul class="nav nav-tabs mb-4">
        <li class="nav-item">
            <a class="nav-link @(!isSalesArchived ? "active fw-bold" : "")" href="?archived=0">
                <i class="bi bi-graph-up me-1"></i> Active Orders
            </a>
        </li>
        <li class="nav-item">
            <a class="nav-link @(isSalesArchived ? "active fw-bold" : "")" href="?archived=1">
                <i class="bi bi-archive me-1"></i> Archived
            </a>
        </li>
    </ul>

    <!-- Stats Cards -->
    <div class="row mb-4">'''
c = c.replace(old_hdr2, new_hdr2, 1)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
print("Sales done")
