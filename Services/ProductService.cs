using ClientSphere.Models;
using ClientSphere.Repositories;

namespace ClientSphere.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateProductAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            await _repository.AddAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
