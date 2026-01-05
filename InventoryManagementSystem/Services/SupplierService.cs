using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly DatabaseService _db;

        public SupplierService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _db.QueryAsync<Supplier>("SELECT * FROM Suppliers ORDER BY Name");
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _db.QuerySingleAsync<Supplier>("SELECT * FROM Suppliers WHERE SupplierId = @Id", new { Id = id });
        }

        public async Task<Supplier> AddSupplierAsync(Supplier supplier)
        {
            supplier.CreatedDate = DateTime.Now;
            var sql = @"
                INSERT INTO Suppliers (Name, ContactPerson, Email, Phone, Address, CreatedDate)
                VALUES (@Name, @ContactPerson, @Email, @Phone, @Address, @CreatedDate);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            supplier.SupplierId = await _db.ExecuteScalarAsync<int>(sql, supplier);
            return supplier;
        }

        public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
        {
            var sql = @"
                UPDATE Suppliers SET 
                    Name = @Name, ContactPerson = @ContactPerson, Email = @Email, 
                    Phone = @Phone, Address = @Address
                WHERE SupplierId = @SupplierId";
            await _db.ExecuteAsync(sql, supplier);
            return supplier;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            var rows = await _db.ExecuteAsync("DELETE FROM Suppliers WHERE SupplierId = @Id", new { Id = id });
            return rows > 0;
        }
    }
}