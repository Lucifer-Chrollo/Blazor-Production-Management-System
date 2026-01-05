using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface IProductionLogService
    {
        Task<List<ProductionLog>> GetAllLogsAsync();
        Task<List<ProductionLog>> GetLogsByWorkOrderIdAsync(int workOrderId);
        Task<ProductionLog> AddProductionLogAsync(ProductionLog log);
        Task<List<ProductionLog>> GetRecentLogsAsync(int count = 10);
    }
}