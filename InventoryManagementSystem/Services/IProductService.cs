using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<List<Product>> GetProductsByTypeAsync(ProductType productType);
        Task<Product?> GetProductByIdAsync(int id);
        Task<List<Product>> GetLowStockProductsAsync();
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
    }
}