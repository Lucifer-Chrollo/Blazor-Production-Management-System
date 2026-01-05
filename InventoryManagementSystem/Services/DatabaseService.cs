using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace InventoryManagementSystem.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, param);
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL: {Sql}", sql);
                throw;
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, param);
                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value) return default;
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scalar SQL: {Sql}", sql);
                throw;
            }
        }

        public async Task<List<T>> QueryAsync<T>(string sql, object? param = null) where T : new()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, param);

                using var reader = await command.ExecuteReaderAsync();
                var list = new List<T>();
                var properties = typeof(T).GetProperties().Where(p => p.CanWrite).ToList();

                while (await reader.ReadAsync())
                {
                    var item = new T();
                    foreach (var prop in properties)
                    {
                        if (!reader.HasColumn(prop.Name)) continue;
                        var val = reader[prop.Name];
                        if (val != DBNull.Value)
                        {
                            // Basic type conversion handling
                            if (prop.PropertyType.IsEnum)
                            {
                                prop.SetValue(item, Enum.ToObject(prop.PropertyType, val));
                            }
                            else
                            {
                                // Handle Nullable types
                                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                prop.SetValue(item, Convert.ChangeType(val, targetType));
                            }
                        }
                    }
                    list.Add(item);
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {Sql}", sql);
                throw;
            }
        }

        public async Task<T?> QuerySingleAsync<T>(string sql, object? param = null) where T : new()
        {
            var list = await QueryAsync<T>(sql, param);
            return list.FirstOrDefault();
        }

        private void AddParameters(SqlCommand command, object? param)
        {
            if (param == null) return;

            if (param is Dictionary<string, object> dictParam)
            {
                foreach (var kvp in dictParam)
                {
                    command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                }
                return;
            }

            foreach (var prop in param.GetType().GetProperties())
            {
                // Skip navigation properties (collections and complex objects)
                var propType = prop.PropertyType;

                // Skip if it's a collection (ICollection, IEnumerable, List, etc.)
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
                    continue;

                // Skip if it's a complex type (has properties itself) and not a value type or string
                if (propType.IsClass && propType != typeof(string) && !propType.IsPrimitive && propType.Namespace != "System")
                    continue;

                var value = prop.GetValue(param) ?? DBNull.Value;
                command.Parameters.AddWithValue("@" + prop.Name, value);
            }
        }
    }

    public static class DataReaderExtensions
    {
        public static bool HasColumn(this IDataReader dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
