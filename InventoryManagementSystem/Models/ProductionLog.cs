using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class ProductionLog
    {
        [Key]
        public int LogId { get; set; }

        public int WorkOrderId { get; set; }
        public int QuantityProduced { get; set; }
        public DateTime ProductionDate { get; set; } = DateTime.Now;
        public string? OperatorName { get; set; }
        public string? ShiftInfo { get; set; }
        public string? Notes { get; set; }

        // Navigation property
        public WorkOrder WorkOrder { get; set; } = null!;
    }
}