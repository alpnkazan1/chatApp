using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using chatbackend.Repository;
using chatbackend.Data;
using chatbackend.Service;
using chatbackend.Models;

namespace chatbackend.Controllers
{
    /* 
        Files are usually sent through websocket 
            with the only current exception being uploading of avatar
    */
    [ApiController]
    [Route("content")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly FileSystemAccess _fileSystemAccess;
        private readonly ApplicationDBContext _context;
        private readonly MyAuthorizationService _authCheckService;

        public FileController(ILogger<FileController> logger, 
                            FileSystemAccess fileSystemAccess, 
                            ApplicationDBContext context,
                            MyAuthorizationService authCheckService)
        {
            _logger = logger;
            _fileSystemAccess = fileSystemAccess;
            _context = context;
            _authCheckService = authCheckService;
        }

        [HttpGet("file/{folderName}/{fileName}")]
        [Authorize]
        public async Task<IActionResult> GetFile(string folderName, string fileName, long expires, string hash)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the user ID from the authenticated user
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(); // Or a more specific error message
                }

                //Get the file and all of the contents
                string completeFilePath = Path.Combine(_fileSystemAccess.GetBaseFilePath(), folderName, fileName);

                if (!System.IO.File.Exists(completeFilePath))
                {
                    _logger.LogWarning("Wasn't authorized!");
                    return NotFound();
                }

                //if the file is tampered with, then throw forbidden, which is a major red flag: 
                if (!_authCheckService.IsHashValid(folderName, fileName, expires, hash))
                {
                    return Forbid(); //Or Unauthorized.
                }

                if (DateTime.UtcNow > new DateTime(expires))
                {
                    return Unauthorized("Url expired!");
                }

                // Perform authorization check
                if (!await _authCheckService.IsAuthorizedForFile(userId, folderName, fileName, AccessType.Read))
                {
                    return Unauthorized("Cannot access this url!"); // Not authorized to access file
                }

                var fileResult = await _fileSystemAccess.GetFileContent(folderName, fileName, _fileSystemAccess.GetBaseFilePath());
                
                if (fileResult == null)
                {
                    return NotFound();
                }

                return File(fileResult.Content, fileResult.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving file {FolderName}/{FileName}", folderName, fileName);
                return StatusCode(500, "Internal server error.");
            }
        }
        
    }
}