using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<Supplier> AddSupplierAsync(Supplier supplier);
        Task<Supplier> UpdateSupplierAsync(Supplier supplier);
        Task<bool> DeleteSupplierAsync(int id);
    }
}