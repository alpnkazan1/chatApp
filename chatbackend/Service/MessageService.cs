using chatbackend.Data;
using chatbackend.DTOs.Messages;
using chatbackend.Interfaces;
using chatbackend.Models;

public class MessageService : IMessageService
{
    private readonly ApplicationDBContext _dbContext;

    public MessageService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MessageReqDto> SaveMessageAsync(string senderId, MessageSendDto messageDto)
    {
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ChatId = messageDto.ChatId,
            SenderId = Guid.Parse(senderId),
            ReceiverId = messageDto.ReceiverId,
            MessageText = messageDto.MessageText,
            FileFlag = messageDto.FileFlag,
            FileId = messageDto.FileId,
            FileExtension = messageDto.FileExtension,
            Timestamp = DateTime.UtcNow
        };

        await _dbContext.Messages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        return new MessageReqDto
        {
            MessageId = message.MessageId,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            MessageText = message.MessageText,
            FileFlag = message.FileFlag,
            FileId = message.FileId,
            FileExtension = message.FileExtension,
            Timestamp = message.Timestamp,
            FileUrl = message.FileId.HasValue ? $"/api/files/{message.FileId}" : null
        };
    }
}
