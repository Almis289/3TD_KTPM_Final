namespace Book_Store.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public string Sender { get; set; } = ""; // "User" hoặc "Manager"
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public virtual ChatSession ChatSession { get; set; } = null!;
    }
}
