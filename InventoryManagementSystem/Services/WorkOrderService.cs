using InventoryManagementSystem.Models;
using System.Transactions;

namespace InventoryManagementSystem.Services
{
    public class WorkOrderService : IWorkOrderService
    {
        private readonly DatabaseService _db;
        private readonly IBillOfMaterialsService _bomService;
        private readonly IStockTransactionService _transactionService;

        public WorkOrderService(
            DatabaseService db,
            IBillOfMaterialsService bomService,
            IStockTransactionService transactionService)
        {
            _db = db;
            _bomService = bomService;
            _transactionService = transactionService;
        }

        public async Task<List<WorkOrder>> GetAllWorkOrdersAsync()
        {
            var sql = "SELECT * FROM WorkOrders ORDER BY CreatedDate DESC";
            var orders = await _db.QueryAsync<WorkOrder>(sql);
            await PopulateRelations(orders);
            return orders;
        }

        public async Task<WorkOrder?> GetWorkOrderByIdAsync(int id)
        {
            var sql = "SELECT * FROM WorkOrders WHERE WorkOrderId = @Id";
            var order = await _db.QuerySingleAsync<WorkOrder>(sql, new { Id = id });
            if (order != null)
            {
                await PopulateRelations(new List<WorkOrder> { order });
                // Populate logs
                var logs = await _db.QueryAsync<ProductionLog>("SELECT * FROM ProductionLogs WHERE WorkOrderId = @Id", new { Id = id });
                order.ProductionLogs = logs;
            }
            return order;
        }

        public async Task<WorkOrder> AddWorkOrderAsync(WorkOrder workOrder)
        {
            workOrder.CreatedDate = DateTime.Now;
            workOrder.Status = WorkOrderStatus.Pending;
            workOrder.QuantityProduced = 0;
            workOrder.TotalCost = await CalculateTotalCostAsync(workOrder.ProductId, workOrder.QuantityOrdered);

            var sql = @"
                INSERT INTO WorkOrders (
                    OrderNumber, ProductId, QuantityOrdered, DurationMinutes, QuantityProduced, TotalCost, 
                    Status, StartDate, CompletionDate, Notes, BuildReference, CreatedDate
                ) VALUES (
                    @OrderNumber, @ProductId, @QuantityOrdered, @DurationMinutes, @QuantityProduced, @TotalCost, 
                    @Status, @StartDate, @CompletionDate, @Notes, @BuildReference, @CreatedDate
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";

            workOrder.WorkOrderId = await _db.ExecuteScalarAsync<int>(sql, workOrder);
            return workOrder;
        }

        public async Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder)
        {
            workOrder.LastUpdated = DateTime.Now;
            workOrder.TotalCost = await CalculateTotalCostAsync(workOrder.ProductId, workOrder.QuantityOrdered);

            var sql = @"
                UPDATE WorkOrders SET 
                    OrderNumber = @OrderNumber, ProductId = @ProductId, QuantityOrdered = @QuantityOrdered, 
                    DurationMinutes = @DurationMinutes, QuantityProduced = @QuantityProduced, TotalCost = @TotalCost, 
                    Status = @Status, StartDate = @StartDate, CompletionDate = @CompletionDate, 
                    Notes = @Notes, BuildReference = @BuildReference, LastUpdated = @LastUpdated
                WHERE WorkOrderId = @WorkOrderId";

            await _db.ExecuteAsync(sql, workOrder);
            return workOrder;
        }

        public async Task<bool> DeleteWorkOrderAsync(int id)
        {
            var rows = await _db.ExecuteAsync("DELETE FROM WorkOrders WHERE WorkOrderId = @Id", new { Id = id });
            return rows > 0;
        }

        public async Task<List<WorkOrder>> GetActiveWorkOrdersAsync()
        {
            var sql = "SELECT * FROM WorkOrders WHERE Status IN (@S1, @S2)";
            var parameters = new Dictionary<string, object>
            {
                { "S1", (int)WorkOrderStatus.Pending },
                { "S2", (int)WorkOrderStatus.InProgress }
            };

            var orders = await _db.QueryAsync<WorkOrder>(sql, parameters);
            await PopulateRelations(orders);
            return orders;
        }

        public async Task<WorkOrder> StartWorkOrderAsync(int workOrderId)
        {
            var order = await GetWorkOrderByIdAsync(workOrderId);
            if (order == null) throw new Exception("Work order not found");

            order.Status = WorkOrderStatus.InProgress;
            order.StartDate = DateTime.Now;
            order.LastUpdated = DateTime.Now;

            await UpdateWorkOrderAsync(order);
            return order;
        }

        public async Task<WorkOrder> CompleteWorkOrderAsync(int workOrderId)
        {
            var workOrder = await GetWorkOrderByIdAsync(workOrderId);
            if (workOrder == null) throw new Exception("Work order not found");

            // Wrap in TransactionScope for atomicity
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var boms = await _bomService.GetBOMByProductIdAsync(workOrder.ProductId);

                foreach (var bom in boms)
                {
                    var quantityToConsume = (int)(bom.QuantityRequired * workOrder.QuantityOrdered);

                    await _transactionService.AddTransactionAsync(new StockTransaction
                    {
                        ProductId = bom.RawMaterialId,
                        Type = TransactionType.StockOut,
                        Quantity = quantityToConsume,
                        Reference = $"WO-{workOrder.OrderNumber}",
                        Notes = $"Consumed for production of {workOrder.Product?.Name}"
                    });
                }

                // Recalculate Total Cost based on current material prices
                var currentTotalCost = await CalculateTotalCostAsync(workOrder.ProductId, workOrder.QuantityOrdered);
                workOrder.TotalCost = currentTotalCost;

                // Update WAC (Weighted Average Cost) for the Finished Good
                var product = await _db.QuerySingleAsync<Product>("SELECT * FROM Products WHERE ProductId = @Id", new { Id = workOrder.ProductId });
                if (product != null)
                {
                    var currentQty = product.Quantity;
                    var currentCost = product.PurchasePrice;
                    var producedQty = workOrder.QuantityOrdered;

                    decimal newCost = currentCost;

                    // WAC Formula: ((OldQty * OldCost) + (NewQty * NewCost)) / (OldQty + NewQty)
                    // Note: currentQty could be negative if oversold, handle gracefully
                    var effectiveQty = currentQty < 0 ? 0 : currentQty;

                    if (effectiveQty + producedQty > 0)
                    {
                        var oldVal = effectiveQty * currentCost;
                        var newVal = currentTotalCost;
                        newCost = (oldVal + newVal) / (effectiveQty + producedQty);
                    }
                    else
                    {
                        newCost = currentTotalCost / (producedQty > 0 ? producedQty : 1);
                    }

                    // Update Product Cost (Both Price fields for compatibility)
                    await _db.ExecuteAsync("UPDATE Products SET PurchasePrice = @Price, Price = @Price WHERE ProductId = @Id",
                        new { Price = newCost, Id = workOrder.ProductId });
                }

                await _transactionService.AddTransactionAsync(new StockTransaction
                {
                    ProductId = workOrder.ProductId,
                    Type = TransactionType.StockIn,
                    Quantity = workOrder.QuantityOrdered,
                    Reference = $"WO-{workOrder.OrderNumber}",
                    Notes = "Production completed"
                });

                workOrder.Status = WorkOrderStatus.Completed;
                workOrder.QuantityProduced = workOrder.QuantityOrdered;
                workOrder.CompletionDate = DateTime.Now;
                workOrder.LastUpdated = DateTime.Now;

                await UpdateWorkOrderAsync(workOrder);

                scope.Complete();
            }

            return workOrder;
        }

        public async Task<List<WorkOrderDetailViewModel>> GetWorkOrderDetailsAsync(int productId, int quantity)
        {
            var details = new List<WorkOrderDetailViewModel>();
            var boms = await _bomService.GetBOMByProductIdAsync(productId);
            // We need prices for Raw Materials. 
            // _bomService.GetBOMByProductIdAsync populates RawMaterial inside BOM usually?
            // Yes, my rewrite of BillOfMaterialsService populates Relations.

            foreach (var bom in boms)
            {
                var rawMaterial = bom.RawMaterial;
                if (rawMaterial == null) continue; // Should not happen

                var qtyNeeded = bom.QuantityRequired * quantity;
                var priceToCheck = rawMaterial.PurchasePrice > 0 ? rawMaterial.PurchasePrice : rawMaterial.Price;
                var totalPrice = priceToCheck * qtyNeeded;

                details.Add(new WorkOrderDetailViewModel
                {
                    ItemCode = rawMaterial.SKU,
                    Description = rawMaterial.Name,
                    Type = rawMaterial.ProductType.ToString(),
                    PerItemQty = bom.QuantityRequired,
                    UnitOfMeasure = bom.Unit,
                    QtyOnHand = rawMaterial.Quantity,
                    QtyNeeded = qtyNeeded,
                    PricePerItem = priceToCheck,
                    TotalPrice = totalPrice,
                    IsAvailable = rawMaterial.Quantity >= qtyNeeded,
                    ProductId = rawMaterial.ProductId
                });
            }
            return details;
        }

        public async Task<decimal> CalculateTotalCostAsync(int productId, int quantity)
        {
            var details = await GetWorkOrderDetailsAsync(productId, quantity);
            return details.Sum(d => d.TotalPrice);
        }

        private async Task PopulateRelations(List<WorkOrder> orders)
        {
            if (!orders.Any()) return;
            var productIds = orders.Select(o => o.ProductId).Distinct().ToList();

            if (!productIds.Any()) return;

            // Parameterized IN Clause
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

            foreach (var o in orders)
            {
                if (prodDict.ContainsKey(o.ProductId)) o.Product = prodDict[o.ProductId];
            }
        }
        public async Task RecalculateHistoricalCostsAsync()
        {
            var sql = "SELECT * FROM WorkOrders WHERE Status = @Status ORDER BY CompletionDate DESC";
            var completedOrders = await _db.QueryAsync<WorkOrder>(sql, new { Status = (int)WorkOrderStatus.Completed });

            // Group by Product to find the latest run for each
            var latestOrders = completedOrders
                .GroupBy(o => o.ProductId)
                .Select(g => g.OrderByDescending(o => o.CompletionDate).First())
                .ToList();

            foreach (var order in latestOrders)
            {
                decimal unitCost = 0;

                // If TotalCost is missing (legacy data), calculate it now based on current BOM prices
                if (order.TotalCost <= 0)
                {
                    order.TotalCost = await CalculateTotalCostAsync(order.ProductId, order.QuantityOrdered);
                }

                if (order.QuantityProduced > 0)
                {
                    unitCost = order.TotalCost / order.QuantityProduced;
                }

                if (unitCost > 0)
                {
                    await _db.ExecuteAsync("UPDATE Products SET PurchasePrice = @Price, Price = @Price WHERE ProductId = @Id",
                        new { Price = unitCost, Id = order.ProductId });
                }
            }
        }
    }
}