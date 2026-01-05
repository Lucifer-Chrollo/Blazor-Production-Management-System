using InventoryManagementSystem.Components;
using InventoryManagementSystem.Services;
using Radzen;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

// Register DatabaseService
builder.Services.AddScoped<DatabaseService>();

// Register Application Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStockTransactionService, StockTransactionService>();
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
builder.Services.AddScoped<IProductionLogService, ProductionLogService>();
builder.Services.AddScoped<IBillOfMaterialsService, BillOfMaterialsService>();

var app = builder.Build();

// Database Setup
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var connectionString = config.GetConnectionString("DefaultConnection");

    try
    {
        var builderStr = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builderStr.InitialCatalog;

        // 1. Ensure Database Exists (Connect to master)
        builderStr.InitialCatalog = "master";
        using (var masterConn = new SqlConnection(builderStr.ConnectionString))
        {
            masterConn.Open();
            using var cmd = masterConn.CreateCommand();
            cmd.CommandText = $"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{databaseName}') CREATE DATABASE [{databaseName}]";
            cmd.ExecuteNonQuery();
            logger.LogInformation($"Database '{databaseName}' check/creation completed.");
        }

        // 2. Execute Schema Script
        var schemaPath = Path.Combine(app.Environment.ContentRootPath, "schema.sql");
        if (File.Exists(schemaPath))
        {
            var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var script = File.ReadAllText(schemaPath);
            var commands = script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command)) continue;
                try
                {
                    await dbService.ExecuteAsync(command);
                }
                catch (SqlException ex)
                {
                    // Ignore "Database 'InventoryDB' already exists" errors if they slip through, 
                    // or other non-critical script errors (like table already exists handled by IF NOT EXISTS in script)
                    logger.LogWarning(ex, "Error executing SQL command block: {Command}", command.Substring(0, Math.Min(command.Length, 50)));
                }
            }
            logger.LogInformation("Database schema applied successfully.");
        }
        else
        {
            logger.LogWarning("schema.sql not found at {Path}", schemaPath);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Critical error during database setup.");
        // We continue, but the app might fail if DB is missing
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
