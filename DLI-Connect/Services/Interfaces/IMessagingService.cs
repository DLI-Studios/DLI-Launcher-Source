using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IMessagingService
{
    Task<List<ConversationInfo>> GetConversationsAsync();
    Task<List<Message>> GetMessagesAsync(string conversationId);
    Task<List<Message>> GetMessagesAsync(string conversationId, int limit, long beforeCreatedAt = 0);
    Task SendMessageAsync(ConversationInfo conversation, string text, string? messageId = null);
    Task DeleteMessageAsync(ConversationInfo conversation, string messageId);
    Task SetTypingAsync(string conversationId);
    Task<bool> IsPeerTypingAsync(ConversationInfo conversation);
    Task MarkReadAsync(ConversationInfo conversation, List<Message> messages);
    Task<ConversationInfo> GetOrCreateConversationAsync(string peerUid);
    string GenerateMessageId();
}