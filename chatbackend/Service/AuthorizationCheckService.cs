using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using chatbackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace chatbackend.Service
{
    public class AuthorizationCheckService
    {

        private readonly ILogger _logger;
        private readonly string _baseFilePath;
        private readonly string _urlSigningKey;
        private readonly IUrlHelper _urlHelper;
        private readonly ApplicationDBContext _context;

        public AuthorizationCheckService(ILogger<AuthorizationCheckService> logger, string baseFilePath, 
                                string urlSigningKey, IUrlHelper urlHelper, ApplicationDBContext context)
        {
            _logger = logger;
            _baseFilePath = baseFilePath;
            _urlSigningKey = urlSigningKey;
            _urlHelper = urlHelper;
            _context = context;
        }
        public string SignUrl(string url, string key)
        {
            var encoding = new ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = encoding.GetBytes(url);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                string hash = string.Concat(hashmessage.Select(b => b.ToString("x2")));
                return url + "&hash=" + hash;
            }
        }
        public string GenerateSecuredFileURL(string folderName, string fileNameWithExtension, int expirationHours = 1)
        {
            var expirationTime = DateTime.UtcNow.AddHours(expirationHours);
            var key = Guid.NewGuid().ToString(); // Create accessKey

            // Create the URL
            var url = _urlHelper.Action("GetFile", "Content", new { folderName = _baseFilePath + "/" + folderName, fileName = fileNameWithExtension, accessKey = key, expires = expirationTime.Ticks }, "https");

            //Sign the url
            var signedUrl = SignUrl(url, _urlSigningKey);

            return signedUrl;
        }

        public string? GenerateSecuredAvatarURL(string avatarId, int expirationHours = 24)
        {
                if (avatarId == "") return null;
                string folderName = "avatars"; // Avatar files will be in this folder.
                string fileNameWithExtension = avatarId + ".png"; // You can modify the extensions later

                var expirationTime = DateTime.UtcNow.AddHours(expirationHours);
                var key = Guid.NewGuid().ToString(); // Create acessKey

                var url = _urlHelper.Action("GetFile", "Content", new {folderName = _baseFilePath + "/" + folderName, fileName = fileNameWithExtension, accessKey = key, expires = expirationTime.Ticks}, "https");

                //Sign the url
                var signedUrl = SignUrl(url, _urlSigningKey);

                return signedUrl;
        }

        public bool IsHashValid(string folderName, string fileName, string accessKey, long expires, string hash)
        {
            // Create original url, notice parameters order must match the original one during signature creation
            string url = _urlHelper.Action("GetFile", "Content", new { folderName = folderName, fileName = fileName, accessKey = accessKey, expires = expires }, "https");

            // Create the signature
            string signature = SignUrl(url, _urlSigningKey);
            return signature == hash;
        }

        public async Task<bool> IsAuthorizedForChat(string userId, Guid chatId)
        {
            // If user is authorized for a single message they are authorized for all messages.
            var chat = await _context.Chats
                .FirstOrDefaultAsync(m => m.ChatId == chatId && (m.User1Id.ToString() == userId || m.User2Id.ToString() == userId));

            return chat != null;
        }

        public async Task<bool> IsAuthorizedForMessage(string userId, Guid messageId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && (m.SenderId.ToString() == userId || m.ReceiverId.ToString() == userId));

            return message != null;
        }

        public async Task<bool> IsAuthorizedForFile(string userId, string folderName, string fileName, uint accessType)
        {
            /*
                For accessTypes:
                {
                    Read = 0
                    Write = 1
                    Full = 2
                }
            */
            string fileId = Path.GetFileNameWithoutExtension(fileName);

            var file = await _context.ACL
                .FirstOrDefaultAsync(e => e.FileId.ToString() == fileId && e.FolderName.ToString() == folderName && e.UserId.ToString() == userId && (uint)e.AccessType == accessType);

            return file != null;
        }
    }
}