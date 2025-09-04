using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Store.Models
{
    public class ProductDetail
    {
        [Key]
        public int ProductDetailId { get; set; }

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public string Publisher { get; set; } = null!;

        public int PageCount { get; set; }

        [Required]
        public string Language { get; set; } = null!;

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;
    }
}
