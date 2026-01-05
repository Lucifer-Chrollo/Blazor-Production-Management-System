using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public class ProductService : IProductService
    {
        private readonly DatabaseService _db;

        public ProductService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            // We need to join Categories and Suppliers to fill the navigation properties if needed
            // But usually for a grid we might just need the names. 
            // The simple generic mapper won't do nested objects (Category.Name). 
            // So we will modify the query to select into a flat structure or just select * and lazy load (not possible here)
            // or just load IDs.
            // For now, let's load the main fields. If we need "CategoryName", we should probably extend the model or use a View Model.
            // The existing `Product` model has `Category` property.

            // NOTE: The simple mapper in DatabaseService doesn't handle nested objects like `Category`. 
            // We will fetch the products, and maybe fetching categories/suppliers is separate or we join and map manually.
            // Given the complexity of manually mapping nested objects without Dapper, 
            // I will fetch the Products and for the UI to show Category Name, 
            // I will rely on the `CategoryId` being populated and the UI can lookup from a cached list of categories 
            // OR I will modify the `Product` class to have `CategoryName` [NotMapped] but that changes the model.

            // Standard approach without EF:
            // Fetch Products.

            var products = await _db.QueryAsync<Product>("SELECT * FROM Products");

            // For the UI to show Category Name, we might need to populate `Category` object.
            // Let's do a separate fetch for Categories and Suppliers and stitch them in memory? 
            // Or just join in SQL and map manually?
            // "Revamp...use radzen". Radzen DataGrid can accept a flat list. 
            // But existing code uses `p.Category.Name`.
            // I should Populate the `Category` object.

            var categories = await _db.QueryAsync<Category>("SELECT * FROM Categories");
            var suppliers = await _db.QueryAsync<Supplier>("SELECT * FROM Suppliers");

            var catDict = categories.ToDictionary(c => c.CategoryId);
            var supDict = suppliers.ToDictionary(s => s.SupplierId);

            foreach (var p in products)
            {
                if (p.CategoryId.HasValue && catDict.ContainsKey(p.CategoryId.Value)) p.Category = catDict[p.CategoryId.Value];
                if (p.SupplierId.HasValue && supDict.ContainsKey(p.SupplierId.Value)) p.Supplier = supDict[p.SupplierId.Value];
            }

            return products;
        }

        public async Task<List<Product>> GetProductsByTypeAsync(ProductType productType)
        {
            var products = await _db.QueryAsync<Product>("SELECT * FROM Products WHERE ProductType = @Type", new { Type = (int)productType });

            // Populate relations
            var categories = await _db.QueryAsync<Category>("SELECT * FROM Categories");
            var suppliers = await _db.QueryAsync<Supplier>("SELECT * FROM Suppliers");
            var catDict = categories.ToDictionary(c => c.CategoryId);
            var supDict = suppliers.ToDictionary(s => s.SupplierId);

            foreach (var p in products)
            {
                if (p.CategoryId.HasValue && catDict.ContainsKey(p.CategoryId.Value)) p.Category = catDict[p.CategoryId.Value];
                if (p.SupplierId.HasValue && supDict.ContainsKey(p.SupplierId.Value)) p.Supplier = supDict[p.SupplierId.Value];
            }
            return products;
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var product = await _db.QuerySingleAsync<Product>("SELECT * FROM Products WHERE ProductId = @Id", new { Id = id });
            if (product != null)
            {
                // Populate category/supplier
                if (product.CategoryId.HasValue && product.CategoryId.Value > 0)
                    product.Category = await _db.QuerySingleAsync<Category>("SELECT * FROM Categories WHERE CategoryId = @Id", new { Id = product.CategoryId.Value });

                if (product.SupplierId.HasValue)
                    product.Supplier = await _db.QuerySingleAsync<Supplier>("SELECT * FROM Suppliers WHERE SupplierId = @Id", new { Id = product.SupplierId.Value });
            }
            return product;
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            var products = await _db.QueryAsync<Product>("SELECT * FROM Products WHERE Quantity <= MinimumStock");
            // Populate relations if needed... skipping for brevity unless UI breaks
            var categories = await _db.QueryAsync<Category>("SELECT * FROM Categories");
            var catDict = categories.ToDictionary(c => c.CategoryId);
            foreach (var p in products)
            {
                if (p.CategoryId.HasValue && catDict.ContainsKey(p.CategoryId.Value)) p.Category = catDict[p.CategoryId.Value];
            }
            return products;
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            product.CreatedDate = DateTime.Now;
            var sql = @"
                INSERT INTO Products (Name, SKU, Description, PurchasePrice, SalePrice, SalePriceWithGST, Price, Quantity, MinimumStock, StockCardNo, CategoryId, SupplierId, Location, ProductModel, ItemNature, ProductType, IsKPI, UOM, CreatedDate)
                VALUES (@Name, @SKU, @Description, @PurchasePrice, @SalePrice, @SalePriceWithGST, @Price, @Quantity, @MinimumStock, @StockCardNo, @CategoryId, @SupplierId, @Location, @ProductModel, @ItemNature, @ProductType, @IsKPI, @UOM, @CreatedDate);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, product);
            product.ProductId = id;
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            product.LastUpdated = DateTime.Now;
            var sql = @"
                UPDATE Products SET 
                    Name = @Name, SKU = @SKU, Description = @Description, 
                    PurchasePrice = @PurchasePrice, SalePrice = @SalePrice, SalePriceWithGST = @SalePriceWithGST, Price = @Price, 
                    Quantity = @Quantity, MinimumStock = @MinimumStock, StockCardNo = @StockCardNo, 
                    CategoryId = @CategoryId, SupplierId = @SupplierId, Location = @Location, 
                    ProductModel = @ProductModel, ItemNature = @ItemNature, ProductType = @ProductType, 
                    IsKPI = @IsKPI, UOM = @UOM, LastUpdated = @LastUpdated
                WHERE ProductId = @ProductId";
            await _db.ExecuteAsync(sql, product);
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var rows = await _db.ExecuteAsync("DELETE FROM Products WHERE ProductId = @Id", new { Id = id });
            return rows > 0;
        }
    }
}