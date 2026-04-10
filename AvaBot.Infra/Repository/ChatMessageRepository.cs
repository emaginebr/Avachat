using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class ChatMessageRepository : IChatMessageRepository<ChatMessage>
{
    private readonly AvaBotContext _context;

    public ChatMessageRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessage>> GetBySessionIdAsync(long sessionId, int page, int pageSize)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountBySessionIdAsync(long sessionId)
    {
        return await _context.ChatMessages.CountAsync(m => m.ChatSessionId == sessionId);
    }

    public async Task<List<ChatMessage>> GetRecentBySessionIdAsync(long sessionId, int count)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        message.CreatedAt = DateTime.UtcNow;
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<List<ChatMessage>> GetLastBySessionIdAsync(long sessionId, int count = 10)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
