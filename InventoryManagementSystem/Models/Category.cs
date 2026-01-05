using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
