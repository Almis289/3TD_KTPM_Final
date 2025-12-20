namespace Book_Store.Models
{
    public class EmailSettings
    {
        public string FromEmail { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
    }
}

