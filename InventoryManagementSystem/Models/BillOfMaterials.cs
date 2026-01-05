using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class BillOfMaterials
    {
        [Key]
        public int BOMId { get; set; }

        public int FinishedProductId { get; set; }
        public int RawMaterialId { get; set; }
        public decimal QuantityRequired { get; set; }
        public string Unit { get; set; } = "pcs";
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }

        // Navigation properties
        public Product FinishedProduct { get; set; } = null!;
        public Product RawMaterial { get; set; } = null!;
    }
}