import re

# 1. Fix Dashboard Revenue peso symbol
path = "d:/VS REPO/ClientSphere/Views/Admin/Dashboard.cshtml"
with open(path, encoding="utf-8") as f:
    c = f.read()
c_new = c.replace("?@Model.TotalRevenue", "\u20b1@Model.TotalRevenue")
if c_new != c:
    with open(path, "w", encoding="utf-8") as f:
        f.write(c_new)
    print("Fixed Dashboard peso")
else:
    print("Dashboard peso - no change needed")

# 2. Fix any remaining ?@ peso signs in all view files
import os, glob
views_dir = "d:/VS REPO/ClientSphere/Views"
fixed_files = []
for filepath in glob.glob(views_dir + "/**/*.cshtml", recursive=True):
    with open(filepath, encoding="utf-8") as f:
        c = f.read()
    if "?@" in c:
        c_new = c.replace("?@", "\u20b1@")
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(c_new)
        fixed_files.append(filepath)
if fixed_files:
    for fp in fixed_files:
        print(f"Fixed peso in: {fp}")
else:
    print("No remaining ?@ peso issues in views")

print("All done")
