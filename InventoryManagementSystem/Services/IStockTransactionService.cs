using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface IStockTransactionService
    {
        Task<List<StockTransaction>> GetAllTransactionsAsync();
        Task<List<StockTransaction>> GetTransactionsByProductIdAsync(int productId);
        Task<StockTransaction> AddTransactionAsync(StockTransaction transaction);
        Task<List<StockTransaction>> GetRecentTransactionsAsync(int count = 10);
    }
}