using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.Models
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }

        [Required, MaxLength(100)]
        public string AuthorName { get; set; } = null!;

        public string? Bio { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
