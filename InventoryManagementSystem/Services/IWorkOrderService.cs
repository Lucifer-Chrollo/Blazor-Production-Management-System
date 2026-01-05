using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface IWorkOrderService
    {
        Task<List<WorkOrder>> GetAllWorkOrdersAsync();
        Task<WorkOrder?> GetWorkOrderByIdAsync(int id);
        Task<WorkOrder> AddWorkOrderAsync(WorkOrder workOrder);
        Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder);
        Task<bool> DeleteWorkOrderAsync(int id);
        Task<List<WorkOrder>> GetActiveWorkOrdersAsync();
        Task<WorkOrder> CompleteWorkOrderAsync(int workOrderId);
        Task<WorkOrder> StartWorkOrderAsync(int workOrderId);
        Task<List<WorkOrderDetailViewModel>> GetWorkOrderDetailsAsync(int productId, int quantity);
        Task<decimal> CalculateTotalCostAsync(int productId, int quantity);
        Task RecalculateHistoricalCostsAsync();
    }
}