namespace InventoryManagementSystem.Models
{
    public class WorkOrderDetailViewModel
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal PerItemQty { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int QtyOnHand { get; set; }
        public decimal QtyNeeded { get; set; }
        public decimal PricePerItem { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsAvailable { get; set; }
        public int ProductId { get; set; }
    }
}