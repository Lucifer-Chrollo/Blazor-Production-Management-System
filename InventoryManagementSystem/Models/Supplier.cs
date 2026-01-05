using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
