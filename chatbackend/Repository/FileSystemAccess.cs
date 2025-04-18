using chatbackend.Models;

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

        public FileSystemAccess(ILogger<FileSystemAccess> logger, string baseFilePath)
        {
            _logger = logger;
            _baseFilePath = baseFilePath;
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

        public void DeleteFile(string? folderName, string? fileNameWithExtension)
        {
            if(folderName == null || fileNameWithExtension == null) return;

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

        public async Task<bool> UploadFileAsync(IFormFile file, uint fileFlag, Guid fileId)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Invalid file upload attempt: file is null or empty.");
                return false;
            }

            string subFolder = fileFlag switch
            {
                1 => "sounds",
                2 => "images",
                3 => "rest",
                4 => "avatars",
                _ => throw new ArgumentException("Invalid file flag")
            };

            string fileExtension = Path.GetExtension(file.FileName);
            string fileDirectory = Path.Combine(_baseFilePath, subFolder);

            if (!Directory.Exists(fileDirectory))
            {
                _logger.LogError("Directory does not exist: {Directory}", fileDirectory);
                return false;
            }

            string filePath = Path.Combine(fileDirectory, $"{fileId}{fileExtension}");

            try
            {
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                _logger.LogInformation("File saved successfully at {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file at {FilePath}", filePath);
                return false;
            }
        }
    
        public string? GetFileNameWithExtension(string? folderName, string? fileNameWithoutExtension)
        {
            if(folderName == null || fileNameWithoutExtension == null) return null;

            string folderPath = Path.Combine(_baseFilePath, folderName);
            
            if (!Directory.Exists(folderPath))
                return null;

            var matchingFiles = Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.*");
            
            return matchingFiles.Length > 0 ? Path.GetFileName(matchingFiles[0]) : null;
        }   
    }
}