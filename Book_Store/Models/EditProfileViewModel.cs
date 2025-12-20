namespace Book_Store.Models
{
    public class EditProfileViewModel
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }
}
