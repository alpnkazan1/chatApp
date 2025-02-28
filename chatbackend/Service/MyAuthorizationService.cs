using chatbackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using chatbackend.Interfaces;

namespace chatbackend.Service
{
    public class MyAuthorizationService : IAuthorizationService //Implementing Interface
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<MyAuthorizationService> _logger;
        private readonly string _baseFilePath;
        private readonly string _urlSigningKey;
        private readonly IUrlHelper _urlHelper; //IUrlHelper

        public MyAuthorizationService(
            ApplicationDBContext context,
            ILogger<MyAuthorizationService> logger,
            IConfiguration configuration,
            IUrlHelper urlHelper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));

            //Load from Configuration
            _baseFilePath = configuration["FileStorage:BaseFilePath"] ?? throw new ArgumentNullException("FileStorage:BaseFilePath");
            _urlSigningKey = configuration["UrlSigningKey"] ?? throw new ArgumentNullException("UrlSigningKey");
        }

        //URL Signing functions
        private string SignUrl(string url, string key) //Private
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

            // Create the URL
            var url = _urlHelper.Action("GetFile", "Content", new { folderName = _baseFilePath + "/" + folderName, fileName = fileNameWithExtension, expires = expirationTime.Ticks }, "https");

            //Sign the url
            var signedUrl = SignUrl(url, _urlSigningKey);

            return signedUrl;
        }

        public string? GenerateSecuredAvatarURL(string avatarId, int expirationHours = 24)
        {
            if (string.IsNullOrEmpty(avatarId)) return null;
            string folderName = "avatars"; // Avatar files will be in this folder.
            string fileNameWithExtension = avatarId + ".png"; // You can modify the extensions later

            var expirationTime = DateTime.UtcNow.AddHours(expirationHours);

            var url = _urlHelper.Action("GetFile", "Content", new { folderName = _baseFilePath + "/" + folderName, fileName = fileNameWithExtension, expires = expirationTime.Ticks }, "https");

            //Sign the url
            var signedUrl = SignUrl(url, _urlSigningKey);

            return signedUrl;
        }

        public bool IsHashValid(string folderName, string fileName, long expires, string hash)
        {
            // Create original url, notice parameters order must match the original one during signature creation
            string url = _urlHelper.Action("GetFile", "Content", new { folderName = folderName, fileName = fileName, expires = expires }, "https");

            // Create the signature
            string signature = SignUrl(url, _urlSigningKey);
            return signature == hash;
        }

        public async Task<bool> IsAuthorizedForChat(string userId, Guid chatId)
        {
            try {
                // Your existing IsAuthorizedForChat logic from AuthorizationCheckService
                var chat = await _context.Chats
                 .FirstOrDefaultAsync(m => m.ChatId == chatId && (m.User1Id.ToString() == userId || m.User2Id.ToString() == userId));

                bool isAuthorized = chat != null;
                return isAuthorized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during IsAuthorizedForChat");
                return false;  //Or throw, depending on your handling strategy.
            }
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