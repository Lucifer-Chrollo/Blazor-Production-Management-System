using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly DatabaseService _db;

        public CategoryService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _db.QueryAsync<Category>("SELECT * FROM Categories ORDER BY Name");
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _db.QuerySingleAsync<Category>("SELECT * FROM Categories WHERE CategoryId = @Id", new { Id = id });
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            category.CreatedDate = DateTime.Now;
            var sql = @"
                INSERT INTO Categories (Name, Description, CreatedDate)
                VALUES (@Name, @Description, @CreatedDate);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            category.CategoryId = await _db.ExecuteScalarAsync<int>(sql, category);
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            var sql = "UPDATE Categories SET Name = @Name, Description = @Description WHERE CategoryId = @CategoryId";
            await _db.ExecuteAsync(sql, category);
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var rows = await _db.ExecuteAsync("DELETE FROM Categories WHERE CategoryId = @Id", new { Id = id });
            return rows > 0;
        }
    }
}