using System;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.Models
{
    public class SubscribeEmail
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
