using ClientSphere.Models;
using ClientSphere.Repositories;
using ClientSphere.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ClientSphere.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;

        public OrderService(IOrderRepository repository, IProductRepository productRepository, ApplicationDbContext context)
        {
            _repository = repository;
            _productRepository = productRepository;
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            return await _repository.GetByCustomerIdAsync(customerId);
        }

        public async Task CreateOrderAsync(Order order)
        {
            // Calculate total amount from items if not set
            // In a real scenario, we'd fetch product prices again to ensure validity
            decimal total = 0;
            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    // Use current product price
                    item.UnitPrice = product.Price;
                    total += item.UnitPrice * item.Quantity;
                }
            }
            order.TotalAmount = total;
            order.OrderDate = DateTime.UtcNow;
            order.Status = OrderStatus.Pending;

            await _repository.AddAsync(order);
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<SalesManagerStats> GetSalesManagerStatsAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);

            // MTD Revenue
            var currentMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.Status == OrderStatus.Completed)
                .ToListAsync(); // Materialize for simpler calculation if needed, or stick to IQueryable
            
            decimal mtdRevenue = currentMonthOrders.Sum(o => o.TotalAmount);
            int mtdDeals = currentMonthOrders.Count;

            // Last Month Revenue for Growth
            var lastMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth && o.Status == OrderStatus.Completed)
                .ToListAsync();

            decimal lastMonthRevenue = lastMonthOrders.Sum(o => o.TotalAmount);
            int lastMonthDeals = lastMonthOrders.Count;

            // Growth Calculation
            double growth = lastMonthRevenue > 0 ? (double)((mtdRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 : 100;
            double dealGrowth = lastMonthDeals > 0 ? (double)((mtdDeals - lastMonthDeals) / lastMonthDeals) * 100 : 100;

            // Avg Deal Size (All Time or MTD? Let's do MTD)
            decimal avgDealSize = mtdDeals > 0 ? mtdRevenue / mtdDeals : 0;

            // Pending High Value Deals (> $5000 and Pending/Processing)
            var highValue = await _context.Orders
                .Where(o => o.TotalAmount > 5000 && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing))
                .OrderByDescending(o => o.TotalAmount)
                .Take(5)
                .ToListAsync();

            return new SalesManagerStats
            {
                TotalRevenueMTD = mtdRevenue,
                RevenueGrowth = Math.Round(growth, 1),
                QuotaAchievement = 85.0m, // Hardcoded for now as Quotas aren't in DB
                DealsClosedMTD = mtdDeals,
                DealsGrowth = Math.Round(dealGrowth, 1),
                AvgDealSize = Math.Round(avgDealSize, 2),
                PendingHighValueDeals = highValue
            };
        }
    }
}
