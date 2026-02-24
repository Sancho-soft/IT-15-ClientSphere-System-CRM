import re
with open("d:/VS REPO/ClientSphere/Views/Shared/_AdminLayout.cshtml", "r", encoding="utf-8") as f:
    html = f.read()

admin_block = re.search(r'(<div class="small text-uppercase text-muted fw-bold px-4 mb-2 mt-2" style="font-size: 0.75rem;">Administration</div>\s*<nav class="nav flex-column">.*?</nav>)', html, re.DOTALL).group(1)
crm_block = re.search(r'(@if \(User\.IsInRole\("Super Admin"\)\)\s*\{\s*<div class="small text-uppercase text-muted fw-bold px-4 mb-2 mt-4" style="font-size: 0.75rem;">CRM Modules</div>\s*<nav class="nav flex-column mb-3">.*?</nav>\s*\})', html, re.DOTALL).group(1)

html = html.replace(admin_block, "<!-- ADMIN_BLOCK_PLACEHOLDER -->")
html = html.replace(crm_block, admin_block)
html = html.replace("<!-- ADMIN_BLOCK_PLACEHOLDER -->", crm_block)

# Fix spacing so "CRM modules" doesn't have mt-4 on top, but Admin does
html = html.replace('<div class="small text-uppercase text-muted fw-bold px-4 mb-2 mt-2" style="font-size: 0.75rem;">Administration</div>', '<div class="small text-uppercase text-muted fw-bold px-4 mb-2 mt-4" style="font-size: 0.75rem;">Administration</div>')
html = html.replace('<div class="small text-uppercase text-muted fw-bold px-4 mb-2 mt-4" style="font-size: 0.75rem;">CRM Modules</div>', '<div class="small text-uppercase text-muted fw-bold px-4 mb-2 mt-2" style="font-size: 0.75rem;">CRM Modules</div>')

with open("d:/VS REPO/ClientSphere/Views/Shared/_AdminLayout.cshtml", "w", encoding="utf-8") as f:
    f.write(html)

with open("d:/VS REPO/ClientSphere/Views/Admin/Dashboard.cshtml", "r", encoding="utf-8") as f:
    dash = f.read()
dash = dash.replace('<i class="bi bi-currency-dollar h4 mb-0"></i>', '<i class="h4 mb-0">?</i>')
with open("d:/VS REPO/ClientSphere/Views/Admin/Dashboard.cshtml", "w", encoding="utf-8") as f:
    f.write(dash)
print("Done")
