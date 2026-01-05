using InventoryManagementSystem.Models;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.Services
{
    public class ProductionLogService : IProductionLogService
    {
        private readonly DatabaseService _db;

        public ProductionLogService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<ProductionLog>> GetLogsByWorkOrderIdAsync(int workOrderId)
        {
            return await _db.QueryAsync<ProductionLog>("SELECT * FROM ProductionLogs WHERE WorkOrderId = @Id", new { Id = workOrderId });
        }

        public async Task<ProductionLog> AddProductionLogAsync(ProductionLog log)
        {
            log.ProductionDate = DateTime.Now;
            var sql = @"
                INSERT INTO ProductionLogs (WorkOrderId, QuantityProduced, ProductionDate, OperatorName, ShiftInfo, Notes)
                VALUES (@WorkOrderId, @QuantityProduced, @ProductionDate, @OperatorName, @ShiftInfo, @Notes);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            log.LogId = await _db.ExecuteScalarAsync<int>(sql, log);
            return log;
        }

        public async Task<List<ProductionLog>> GetAllLogsAsync()
        {
            return await _db.QueryAsync<ProductionLog>("SELECT * FROM ProductionLogs ORDER BY ProductionDate DESC");
        }

        public async Task<List<ProductionLog>> GetRecentLogsAsync(int count = 10)
        {
            var sql = $"SELECT TOP {count} * FROM ProductionLogs ORDER BY ProductionDate DESC";
            var logs = await _db.QueryAsync<ProductionLog>(sql);

            // Populate Relations (WorkOrder -> Product)
            if (logs.Any())
            {
                var woIds = logs.Select(l => l.WorkOrderId).Distinct().ToList();

                if (woIds.Any())
                {
                    // Parameterized IN for WorkOrders
                    var woParams = new Dictionary<string, object>();
                    var woParamNames = new List<string>();
                    for (int i = 0; i < woIds.Count; i++)
                    {
                        var pn = $"w{i}";
                        woParamNames.Add("@" + pn);
                        woParams.Add(pn, woIds[i]);
                    }
                    var woInClause = string.Join(",", woParamNames); ;

                    var wos = await _db.QueryAsync<WorkOrder>($"SELECT * FROM WorkOrders WHERE WorkOrderId IN ({woInClause})", woParams);
                    var woDict = wos.ToDictionary(w => w.WorkOrderId);

                    // Load Products for these WorkOrders
                    var pIds = wos.Select(w => w.ProductId).Distinct().ToList();

                    Dictionary<int, Product> prodDict = new();
                    if (pIds.Any())
                    {
                        var pParams = new Dictionary<string, object>();
                        var pParamNames = new List<string>();
                        for (int i = 0; i < pIds.Count; i++)
                        {
                            var pn = $"p{i}";
                            pParamNames.Add("@" + pn);
                            pParams.Add(pn, pIds[i]);
                        }
                        var pInClause = string.Join(",", pParamNames); ;

                        var prods = await _db.QueryAsync<Product>($"SELECT * FROM Products WHERE ProductId IN ({pInClause})", pParams);
                        prodDict = prods.ToDictionary(p => p.ProductId);
                    }

                    foreach (var l in logs)
                    {
                        if (woDict.ContainsKey(l.WorkOrderId))
                        {
                            l.WorkOrder = woDict[l.WorkOrderId];
                            if (l.WorkOrder != null && prodDict.ContainsKey(l.WorkOrder.ProductId))
                            {
                                l.WorkOrder.Product = prodDict[l.WorkOrder.ProductId];
                            }
                        }
                    }
                }
            }
            return logs;
        }
    }
}