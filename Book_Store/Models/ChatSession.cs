namespace Book_Store.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public bool IsClosed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual List<ChatMessage> Messages { get; set; } = new();
    }
}
