using chatbackend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;  // Import IWebHostEnvironment
using Microsoft.Extensions.Logging;
using chatbackend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;

namespace chatbackend.Repository
{
    public class FileSystemAccess
    {
        public class FileResult
        {
            public byte[] Content { get; set; }
            public string ContentType { get; set; }
        }

        private readonly ILogger _logger;
        private readonly string _baseFilePath;
        private readonly string _urlSigningKey;
        private readonly IUrlHelper _urlHelper;

        public FileSystemAccess(ILogger<FileSystemAccess> logger, string baseFilePath, 
                                string urlSigningKey, IUrlHelper urlHelper)
        {
            _logger = logger;
            _baseFilePath = baseFilePath;
            _urlSigningKey = urlSigningKey;
            _urlHelper = urlHelper;
        }


        // Public helper method to access base filepath:
        public string GetBaseFilePath() {
          return _baseFilePath;
        }

        public async Task<FileResult?> GetFileContent(string folderName, string fileNameWithExtension, string baseFilePath)
        {
            var filePath = Path.Combine(baseFilePath, folderName, fileNameWithExtension);

            if (!File.Exists(filePath)) return null;

            try
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                string contentType = GetContentType(filePath);

                return new FileResult { Content = fileBytes, ContentType = contentType };
            }
            catch (Exception)
            {
                throw; // Re-throw the exception
            }
        }

        public async Task<bool> IsAuthorizedForFile(ApplicationDBContext context, string userId, string folderName, string fileNameWithExtension)
        {
            string filePath = Path.Combine(folderName, fileNameWithExtension);
            var message = await context.Messages
                .FirstOrDefaultAsync(m => m.FileId.ToString() == filePath && (m.SenderId == userId || m.ReceiverId == userId));

            return message != null;
        }

        public async Task<bool> IsBlocked(ApplicationDBContext context, string user1Id, string user2Id)
        {
            var chat = await context.Chats
                .FirstOrDefaultAsync(c =>
                    ((c.User1Id == user1Id && c.User2Id == user2Id) || (c.User1Id == user2Id && c.User2Id == user1Id)) &&
                    (c.BlockFlag == 1 || c.BlockFlag == 2 || c.BlockFlag == 3));

            return chat != null;
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },
                { ".wav", "audio/wav" },
                { ".mp3", "audio/mpeg" },
                { ".txt", "text/plain" },
                // Add more types as needed
            };

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        public void DeleteFile(string folderName, string fileNameWithExtension)
        {
            string filePath = Path.Combine(_baseFilePath, folderName, fileNameWithExtension);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"Deleted file: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"File not found, cannot delete: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                throw; // Re-throw to let the caller handle the exception
            }
        }

        public string GetSubfolder(uint fileFlag)
        {
            switch (fileFlag)
            {
                case 1:
                    return "sounds";
                case 2:
                    return "images";
                case 3:
                    return "rest";
                default:
                    return ""; // Or throw an exception if an invalid FileFlag is encountered
            }
        }

        public string CreateFilePath(Message message)
        {
            string subfolder = GetSubfolder(message.FileFlag);
            string fileNameWithExtension = message.FileId.ToString() + "." + message.FileExtension;
            return Path.Combine(_baseFilePath, subfolder, fileNameWithExtension);
        }

        public async Task<bool> IsAuthorizedForChat(ApplicationDBContext context, string userId, Guid chatId)
        {
            // If user is authorized for a single message they are authorized for all messages.
            var message = await context.Chats
                .FirstOrDefaultAsync(m => m.ChatId == chatId && (m.User1Id == userId || m.User1Id == userId));

            return message != null;
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

        private string SignUrl(string url, string key)
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
    }
}