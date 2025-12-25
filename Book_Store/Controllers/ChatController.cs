using Book_Store.Data;
using Book_Store.Hubs;
using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Customer")]
public class ChatController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(BookStoreDbContext context, IHubContext<ChatHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    // Load view
    public async Task<IActionResult> Chat()
    {
        string userId = User.FindFirst("UserId")?.Value ?? "";

        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsClosed);

        if (session == null)
        {
            session = new ChatSession { UserId = userId, CreatedAt = DateTime.UtcNow };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        return View("~/Views/Customer/Chat/Chat.cshtml", session);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto dto)
    {
        var msg = new ChatMessage
        {
            ChatSessionId = dto.SessionId,
            Sender = dto.Sender,
            Content = dto.Content,
            SentAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();

        // Gửi tin nhắn cho tất cả client trong session qua SignalR
        await _hub.Clients.Group($"chat_{dto.SessionId}")
            .SendAsync("ReceiveMessage", dto.Sender, dto.Content);

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Close([FromBody] CloseSessionDto dto)
    {
        var session = await _context.ChatSessions.FindAsync(dto.Id);
        if (session != null)
        {
            session.IsClosed = true;
            await _context.SaveChangesAsync();
        }

        // Tạo session mới
        var newSession = new ChatSession { UserId = session.UserId, CreatedAt = DateTime.UtcNow };
        _context.ChatSessions.Add(newSession);
        await _context.SaveChangesAsync();

        return Json(new { newSessionId = newSession.Id });
    }
    public async Task<IActionResult> Widget()
    {
        string userId = User.FindFirst("UserId")?.Value ?? "";

        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsClosed);

        if (session == null)
        {
            session = new ChatSession
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        return View("~/Views/Customer/Chat/Widget.cshtml", session);
    }
}