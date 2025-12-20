namespace Book_Store.Models
{
    public class ChatMessageDto
    {
        public int SessionId { get; set; }
        public string Sender { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
