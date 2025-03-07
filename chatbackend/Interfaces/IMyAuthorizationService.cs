using chatbackend.Models;

namespace chatbackend.Interfaces
{
    public interface IMyAuthorizationService
    {
        string GenerateSecuredFileURL(string folderName, string fileNameWithExtension, int expirationHours = 1);
        string? GenerateSecuredAvatarURL(string avatarId, int expirationHours = 24);
        bool IsHashValid(string folderName, string fileName, long expires, string hash);
        Task<bool> IsAuthorizedForChat(string userId, Guid chatId);
        Task<bool> IsAuthorizedForMessage(string userId, Guid messageId);
        Task<bool> IsAuthorizedForFile(string userId, string folderName, string fileName, AccessType accessType);
        Task<bool> IsBlocked(string user1Id, string user2Id);
    }
}