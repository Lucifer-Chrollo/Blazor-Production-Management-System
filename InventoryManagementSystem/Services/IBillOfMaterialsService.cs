using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface IBillOfMaterialsService
    {
        Task<List<BillOfMaterials>> GetAllBOMsAsync();
        Task<List<BillOfMaterials>> GetBOMByProductIdAsync(int productId);
        Task<BillOfMaterials?> GetBOMByIdAsync(int bomId);
        Task<BillOfMaterials> AddBOMAsync(BillOfMaterials bom);
        Task<BillOfMaterials> UpdateBOMAsync(BillOfMaterials bom);
        Task<bool> DeleteBOMAsync(int bomId);
        Task<bool> CanProduceAsync(int productId, int quantity);
        Task<List<Product>> GetFinishedProductsAsync();
        Task<List<Product>> GetRawMaterialsAsync();
        Task<bool> BOMExistsAsync(int finishedProductId, int rawMaterialId);


    }
}