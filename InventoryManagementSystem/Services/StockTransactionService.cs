using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public class StockTransactionService : IStockTransactionService
    {
        private readonly DatabaseService _db;

        public StockTransactionService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<StockTransaction>> GetAllTransactionsAsync()
        {
            var sql = "SELECT * FROM StockTransactions ORDER BY TransactionDate DESC";
            var transactions = await _db.QueryAsync<StockTransaction>(sql);
            await PopulateRelations(transactions);
            return transactions;
        }

        public async Task<List<StockTransaction>> GetTransactionsByProductIdAsync(int productId)
        {
            var sql = "SELECT * FROM StockTransactions WHERE ProductId = @Id ORDER BY TransactionDate DESC";
            var transactions = await _db.QueryAsync<StockTransaction>(sql, new { Id = productId });
            await PopulateRelations(transactions);
            return transactions;
        }

        public async Task<StockTransaction> AddTransactionAsync(StockTransaction transaction)
        {
            transaction.TransactionDate = DateTime.Now;

            // We need to update Product quantity and Insert Transaction in one go
            var sql = @"
            BEGIN TRANSACTION;
                INSERT INTO StockTransactions (ProductId, Type, Quantity, Reference, Notes, TransactionDate)
                VALUES (@ProductId, @Type, @Quantity, @Reference, @Notes, @TransactionDate);
                
                DECLARE @NewQty int;
                
                -- Determine the change
                -- Type is Enum: StockIn=0, StockOut=1, Adjustment=2
                -- We have to handle handling conditional logic in SQL
                
                IF @Type = 0 -- StockIn
                    UPDATE Products SET Quantity = Quantity + @Quantity, LastUpdated = GETDATE() WHERE ProductId = @ProductId;
                ELSE IF @Type = 1 -- StockOut
                    UPDATE Products SET Quantity = Quantity - @Quantity, LastUpdated = GETDATE() WHERE ProductId = @ProductId;
                ELSE IF @Type = 2 -- Adjustment
                    UPDATE Products SET Quantity = @Quantity, LastUpdated = GETDATE() WHERE ProductId = @ProductId;
            COMMIT;
            SELECT CAST(SCOPE_IDENTITY() as int);";

            transaction.TransactionId = await _db.ExecuteScalarAsync<int>(sql, transaction);
            return transaction;
        }

        public async Task<List<StockTransaction>> GetRecentTransactionsAsync(int count = 10)
        {
            // SQL Server: SELECT TOP (@Count) ...
            // ExecuteScalar/Query doesn't support @Count directly in TOP in older versions, but current versions do.
            // Or safer: string interpolation for LIMIT/TOP if param fails.
            // "SELECT top (@Count) *" works in modern SQL Server.
            var sql = $"SELECT TOP {count} * FROM StockTransactions ORDER BY TransactionDate DESC";
            var transactions = await _db.QueryAsync<StockTransaction>(sql);
            await PopulateRelations(transactions);
            return transactions;
        }

        private async Task PopulateRelations(List<StockTransaction> transactions)
        {
            if (!transactions.Any()) return;
            var productIds = transactions.Select(t => t.ProductId).Distinct().ToList();
            if (!productIds.Any()) return;

            var parameters = new Dictionary<string, object>();
            var paramNames = new List<string>();

            for (int i = 0; i < productIds.Count; i++)
            {
                var paramName = $"p{i}";
                paramNames.Add("@" + paramName);
                parameters.Add(paramName, productIds[i]);
            }

            var inClause = string.Join(",", paramNames);
            var products = await _db.QueryAsync<Product>($"SELECT * FROM Products WHERE ProductId IN ({inClause})", parameters);
            var prodDict = products.ToDictionary(p => p.ProductId);

            foreach (var t in transactions)
            {
                if (prodDict.ContainsKey(t.ProductId)) t.Product = prodDict[t.ProductId];
            }
        }
    }
}