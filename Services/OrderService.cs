using ClientSphere.Models;
using ClientSphere.Repositories;

namespace ClientSphere.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository repository, IProductRepository productRepository)
        {
            _repository = repository;
            _productRepository = productRepository;
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
            var order = await _repository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _repository.UpdateAsync(order);
            }
        }
    }
}
