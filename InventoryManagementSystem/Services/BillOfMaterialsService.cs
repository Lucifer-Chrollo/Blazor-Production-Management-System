using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public class BillOfMaterialsService : IBillOfMaterialsService
    {
        private readonly DatabaseService _db;

        public BillOfMaterialsService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<BillOfMaterials>> GetAllBOMsAsync()
        {
            var sql = "SELECT * FROM BillOfMaterials WHERE IsActive = 1";
            var boms = await _db.QueryAsync<BillOfMaterials>(sql);

            // Populate Products
            await PopulateRelations(boms);

            return boms.OrderBy(b => b.FinishedProduct?.Name).ThenBy(b => b.RawMaterial?.Name).ToList();
        }

        public async Task<List<BillOfMaterials>> GetBOMByProductIdAsync(int productId)
        {
            var sql = "SELECT * FROM BillOfMaterials WHERE FinishedProductId = @Id AND IsActive = 1";
            var boms = await _db.QueryAsync<BillOfMaterials>(sql, new { Id = productId });
            await PopulateRelations(boms);
            return boms;
        }

        public async Task<BillOfMaterials?> GetBOMByIdAsync(int bomId)
        {
            var sql = "SELECT * FROM BillOfMaterials WHERE BOMId = @Id";
            var bom = await _db.QuerySingleAsync<BillOfMaterials>(sql, new { Id = bomId });
            if (bom != null)
            {
                await PopulateRelations(new List<BillOfMaterials> { bom });
            }
            return bom;
        }

        public async Task<BillOfMaterials> AddBOMAsync(BillOfMaterials bom)
        {
            bom.CreatedDate = DateTime.Now;
            bom.IsActive = true;
            var sql = @"
                INSERT INTO BillOfMaterials (FinishedProductId, RawMaterialId, QuantityRequired, Unit, Notes, IsActive, CreatedDate)
                VALUES (@FinishedProductId, @RawMaterialId, @QuantityRequired, @Unit, @Notes, @IsActive, @CreatedDate);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            bom.BOMId = await _db.ExecuteScalarAsync<int>(sql, bom);
            return bom;
        }

        public async Task<BillOfMaterials> UpdateBOMAsync(BillOfMaterials bom)
        {
            bom.LastUpdated = DateTime.Now;
            var sql = @"
                UPDATE BillOfMaterials SET 
                    FinishedProductId = @FinishedProductId, RawMaterialId = @RawMaterialId, 
                    QuantityRequired = @QuantityRequired, Unit = @Unit, Notes = @Notes, 
                    IsActive = @IsActive, LastUpdated = @LastUpdated
                WHERE BOMId = @BOMId";
            await _db.ExecuteAsync(sql, bom);
            return bom;
        }

        public async Task<bool> DeleteBOMAsync(int bomId)
        {
            // Soft delete
            var sql = "UPDATE BillOfMaterials SET IsActive = 0, LastUpdated = GETDATE() WHERE BOMId = @Id";
            var rows = await _db.ExecuteAsync(sql, new { Id = bomId });
            return rows > 0;
        }

        public async Task<bool> CanProduceAsync(int productId, int quantity)
        {
            var boms = await GetBOMByProductIdAsync(productId);
            foreach (var bom in boms)
            {
                var requiredQuantity = bom.QuantityRequired * quantity;
                // Check raw material stock
                // Assuming raw material is loaded or we check explicitly
                var rawMaterial = await _db.QuerySingleAsync<Product>("SELECT * FROM Products WHERE ProductId = @Id", new { Id = bom.RawMaterialId });

                if (rawMaterial == null || rawMaterial.Quantity < requiredQuantity)
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<List<Product>> GetFinishedProductsAsync()
        {
            var sql = "SELECT * FROM Products WHERE ProductType = @Type ORDER BY Name";
            var products = await _db.QueryAsync<Product>(sql, new { Type = (int)ProductType.Finished });
            // Populate categories if needed for display? usually yes.
            // We can do a quick fetch of categories
            var categories = await _db.QueryAsync<Category>("SELECT * FROM Categories");
            var catDict = categories.ToDictionary(c => c.CategoryId);
            foreach (var p in products) if (p.CategoryId.HasValue && catDict.ContainsKey(p.CategoryId.Value)) p.Category = catDict[p.CategoryId.Value];

            return products;
        }

        public async Task<List<Product>> GetRawMaterialsAsync()
        {
            var sql = "SELECT * FROM Products WHERE ProductType = @Type ORDER BY Name";
            var products = await _db.QueryAsync<Product>(sql, new { Type = (int)ProductType.Assembly });

            var categories = await _db.QueryAsync<Category>("SELECT * FROM Categories");
            var catDict = categories.ToDictionary(c => c.CategoryId);
            foreach (var p in products) if (p.CategoryId.HasValue && catDict.ContainsKey(p.CategoryId.Value)) p.Category = catDict[p.CategoryId.Value];

            return products;
        }

        public async Task<bool> BOMExistsAsync(int finishedProductId, int rawMaterialId)
        {
            var sql = "SELECT COUNT(1) FROM BillOfMaterials WHERE FinishedProductId = @FId AND RawMaterialId = @RId AND IsActive = 1";
            var count = await _db.ExecuteScalarAsync<int>(sql, new { FId = finishedProductId, RId = rawMaterialId });
            return count > 0;
        }

        private async Task PopulateRelations(List<BillOfMaterials> boms)
        {
            if (!boms.Any()) return;

            var productIds = boms.Select(b => b.FinishedProductId)
                            .Union(boms.Select(b => b.RawMaterialId))
                            .Distinct()
                            .ToList();

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

            var categories = await _db.QueryAsync<Category>("SELECT * FROM Categories");
            var catDict = categories.ToDictionary(c => c.CategoryId);

            foreach (var p in products)
            {
                if (p.CategoryId.HasValue && catDict.ContainsKey(p.CategoryId.Value)) p.Category = catDict[p.CategoryId.Value];
            }

            var prodDict = products.ToDictionary(p => p.ProductId);

            foreach (var bom in boms)
            {
                if (prodDict.ContainsKey(bom.FinishedProductId)) bom.FinishedProduct = prodDict[bom.FinishedProductId];
                if (prodDict.ContainsKey(bom.RawMaterialId)) bom.RawMaterial = prodDict[bom.RawMaterialId];
            }
        }
    }
}