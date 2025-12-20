using Book_Store.Data;
using Book_Store.Models;
using Book_Store.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

[Authorize(Roles = "Manager")]
public class ChatManagementController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly IHubContext<ChatHub> _hub;

    public ChatManagementController(BookStoreDbContext context, IHubContext<ChatHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    // Danh sách tất cả session
    public async Task<IActionResult> Index()
    {
        var sessions = await _context.ChatSessions
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View("~/Views/Admin/ChatManagement/Index.cshtml", sessions);
    }

    // Chi tiết 1 session
    public async Task<IActionResult> Details(int id)
    {
        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        return View("~/Views/Admin/ChatManagement/ChatDetails.cshtml", session);
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

        await _hub.Clients.Group($"chat_{dto.SessionId}")
            .SendAsync("ReceiveMessage", dto.Sender, dto.Content);

        return Ok();
    }
}


