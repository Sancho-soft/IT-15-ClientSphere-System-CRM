import re

# Fix "Archived" tab label to "Inactive" in Support, Marketing, Sales, Billing
pages = [
    "d:/VS REPO/ClientSphere/Views/Support/Index.cshtml",
    "d:/VS REPO/ClientSphere/Views/Marketing/Index.cshtml",
    "d:/VS REPO/ClientSphere/Views/Sales/Index.cshtml",
    "d:/VS REPO/ClientSphere/Views/Billing/Index.cshtml",
    "d:/VS REPO/ClientSphere/Views/SupportStaff/Dashboard.cshtml",
]

for path in pages:
    try:
        with open(path, encoding="utf-8") as f:
            c = f.read()
        original = c
        # Replace archive icon + Archived label with inactive icon + Inactive
        c = c.replace(
            '<i class="bi bi-archive me-1"></i> Archived',
            '<i class="bi bi-slash-circle me-1"></i> Inactive'
        )
        if c != original:
            with open(path, "w", encoding="utf-8") as f:
                f.write(c)
            print(f"Fixed: {path}")
        else:
            print(f"No change: {path}")
    except Exception as e:
        print(f"Error {path}: {e}")

# Also fix the tab comment labels "Archive Tabs" -> "Status Tabs"
for path in pages:
    try:
        with open(path, encoding="utf-8") as f:
            c = f.read()
        original = c
        c = c.replace("<!-- Archive Tabs -->", "<!-- Status Tabs -->")
        c = c.replace("<!-- Archive Tabs (Staff/Admin only) -->", "<!-- Status Tabs (Staff/Admin only) -->")
        if c != original:
            with open(path, "w", encoding="utf-8") as f:
                f.write(c)
    except:
        pass

print("Done all")
