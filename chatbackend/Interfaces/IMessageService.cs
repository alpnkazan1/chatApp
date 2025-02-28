using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.DTOs.Messages;

namespace chatbackend.Interfaces
{
public interface IMessageService
{
    Task<MessageReqDto> SaveMessageAsync(string senderId, MessageSendDto messageDto);
}
}