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

namespace chatbackend.Helpers
{
    public static class FileSystemAccess
    {
        public class FileResult
        {
            public byte[] Content { get; set; }
            public string ContentType { get; set; }
        }

        // Static logger instance
        private static ILogger _logger;
        private static string _baseFilePath;

        //Configuration method to initialize the logger
        public static void Configure(ILoggerFactory loggerFactory, string baseFilePath)
        {
            _logger = loggerFactory.CreateLogger("ChatHelper");
            _baseFilePath = baseFilePath;
        }

        public static async Task<FileResult?> GetFileContent(string folderName, string fileNameWithExtension, string baseFilePath)
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

        public static async Task<bool> IsAuthorizedForFile(ApplicationDBContext context, string userId, string folderName, string fileNameWithExtension)
        {
            string filePath = Path.Combine(folderName, fileNameWithExtension);
            var message = await context.Messages
                .FirstOrDefaultAsync(m => m.FileId.ToString() == filePath && (m.SenderId == userId || m.ReceiverId == userId));

            return message != null;
        }

        public static async Task<bool> IsBlocked(ApplicationDBContext context, string user1Id, string user2Id)
        {
            var chat = await context.Chats
                .FirstOrDefaultAsync(c =>
                    ((c.User1Id == user1Id && c.User2Id == user2Id) || (c.User1Id == user2Id && c.User2Id == user1Id)) &&
                    (c.BlockFlag == 1 || c.BlockFlag == 2 || c.BlockFlag == 3));

            return chat != null;
        }

        private static string GetContentType(string path)
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

        public static async Task DeleteFile(string folderName, string fileNameWithExtension)
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

        public static string GetSubfolder(uint fileFlag)
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

        public static string CreateFilePath(Message message)
        {
            string subfolder = GetSubfolder(message.FileFlag);
            string fileNameWithExtension = message.FileId.ToString() + "." + message.FileExtension;
            return Path.Combine(_baseFilePath, subfolder, fileNameWithExtension);
        }
    }
}