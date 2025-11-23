using Book_Store.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.ViewModels
{
    public class EmailMarketingViewModel
    {
        public string Subject { get; set; } = "";
        public string Content { get; set; } = "";

        // Sản phẩm được chọn
        public int? ProductId { get; set; }

        // Danh sách sản phẩm để chọn
        public List<Product> Products { get; set; } = new List<Product>();

        // Danh sách người dùng để chọn gửi mail
        public List<User> Users { get; set; } = new List<User>();

        // Email được chọn
        public List<string> SelectedEmails { get; set; } = new List<string>();
    }
}
