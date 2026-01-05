using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        // Basic Info
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Pricing
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal SalePriceWithGST { get; set; }
        public decimal Price { get; set; } // Keep for backward compatibility

        // Stock
        public int Quantity { get; set; }
        public int MinimumStock { get; set; } = 10;
        public string? StockCardNo { get; set; }

        // Product Details
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string? Location { get; set; }
        public string? ProductModel { get; set; }

        // Item Nature
        public ItemNature ItemNature { get; set; } = ItemNature.Inventory;

        // Product Type
        public ProductType ProductType { get; set; } = ProductType.Finished;

        // Flags
        public bool IsKPI { get; set; }

        // Additional Info
        public string? UOM { get; set; } // Unit of Measure

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }

        // Navigation properties
        public Category? Category { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }

    public enum ProductType
    {
        Finished,       // Finished goods (was General)
        Assembly        // Raw materials
    }

    public enum ItemNature
    {
        Inventory,
        Service
    }
}