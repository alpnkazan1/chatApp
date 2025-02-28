using System;
using System.Threading.Tasks;

namespace chatbackend.Interfaces // Or whatever namespace you want to use
{
    public interface IAuthorizationService
    {
        Task<bool> IsAuthorizedForChat(string userId, Guid chatId); //From AuthorizationCheckService
        string GenerateSecuredFileURL(string folderName, string fileNameWithExtension, int expirationHours = 1); //From AuthorizationCheckService
        string? GenerateSecuredAvatarURL(string avatarId, int expirationHours = 24); //From AuthorizationCheckService
        bool IsHashValid(string folderName, string fileName, long expires, string hash); //From AuthorizationCheckService
        Task<bool> IsAuthorizedForMessage(string userId, Guid messageId); //From AuthorizationCheckService
        Task<bool> IsAuthorizedForFile(string userId, string folderName, string fileName, uint accessType); //From AuthorizationCheckService
    }
}