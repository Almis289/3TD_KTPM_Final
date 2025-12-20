using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "Customer"; // Mặc định là khách hàng

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public string? AvatarUrl { get; set; }


    }
}
