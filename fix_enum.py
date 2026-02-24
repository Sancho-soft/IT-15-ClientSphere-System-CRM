import re

with open("d:/VS REPO/ClientSphere/Controllers/OrdersController.cs", "r", encoding="utf-8") as f:
    orders = f.read()
orders = orders.replace('Status == "Closed" || o.Status == "Cancelled"', 'Status == Models.OrderStatus.Completed || o.Status == Models.OrderStatus.Cancelled')
orders = orders.replace('Status != "Closed" && o.Status != "Cancelled"', 'Status != Models.OrderStatus.Completed && o.Status != Models.OrderStatus.Cancelled')

with open("d:/VS REPO/ClientSphere/Controllers/OrdersController.cs", "w", encoding="utf-8") as f:
    f.write(orders)

print("Done")
