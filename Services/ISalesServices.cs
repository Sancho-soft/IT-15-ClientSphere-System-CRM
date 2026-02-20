using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }

    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(int id);
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task CreateOrderAsync(Order order);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task UpdateOrderAsync(Order order);
        Task<SalesManagerStats> GetSalesManagerStatsAsync();
    }
}
