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
        c = c.replace(
            '<i class="bi bi-slash-circle me-1"></i> Inactive',
            '<i class="bi bi-archive me-1"></i> Archived'
        )
        with open(path, "w", encoding="utf-8") as f:
            f.write(c)
        print(f"Reverted: {path}")
    except Exception as e:
        print(f"Error {path}: {e}")
