
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class WorkOrder
    {
        [Key]
        public int WorkOrderId { get; set; }

        public string OrderNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int QuantityOrdered { get; set; }
        public int DurationMinutes { get; set; } // New field for timer
        public int QuantityProduced { get; set; } = 0;
        public decimal TotalCost { get; set; } = 0;
        public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Pending;
        public DateTime StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string? Notes { get; set; }
        public string? BuildReference { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }

        // Navigation properties
        public Product Product { get; set; } = null!;
        public ICollection<ProductionLog> ProductionLogs { get; set; } = new List<ProductionLog>();
    }

    public enum WorkOrderStatus
    {
        Draft,
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}