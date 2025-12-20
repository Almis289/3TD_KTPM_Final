namespace Book_Store.Models
{
    public class UserProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CartCount { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
