using System;
using System.Collections.Generic;
using Book_Store.Models; // nếu dùng Order, User entity

namespace Book_Store.ViewModels
{
    public class CustomerDetailViewModel
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Order> Orders { get; set; } = new();
    }
}
