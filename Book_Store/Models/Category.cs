using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = null!;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Quan hệ 1 - N với Product
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
