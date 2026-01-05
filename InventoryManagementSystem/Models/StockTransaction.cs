using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class StockTransaction
    {
        [Key]
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // Navigation property
        public Product Product { get; set; } = null!;
    }

    public enum TransactionType
    {
        StockIn,
        StockOut,
        Adjustment
    }
}