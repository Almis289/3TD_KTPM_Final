using Book_Store.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.ViewModels
{
    public class EmailMarketingViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề email")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung email")]
        public string? Content { get; set; }

        // ===== POST DATA =====
        public List<int>? SelectedProductIds { get; set; }
        public List<string>? SelectedEmails { get; set; }
        public string? ExtraEmails { get; set; }

        // ===== VIEW ONLY =====
        public List<string>? SubscribeEmails { get; set; }
        public List<Product>? Products { get; set; }
        public List<User>? Users { get; set; }
    }
}
