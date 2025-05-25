using FileStorageService.Models;
using FileStorageService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using FileStorageService.Data;

namespace FileStorageService.Controllers
{
    /// <summary>
    /// Controller for managing file storage operations
    /// </summary>
    /// <remarks>
    /// This controller handles file upload and retrieval operations
    /// with deduplication based on file hash
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController (
        FileDBStorageService fileDBStorageService,
        FileDriveStorageService fileDriveStorageService,
        ILogger<FilesController> logger): ControllerBase
    {        /// <summary>
        /// Uploads a text file to the storage system
        /// </summary>
        /// <param name="file">The text file to upload</param>
        /// <returns>The ID of the uploaded or existing file</returns>
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            logger.LogInformation("File upload requested: {FileName}, size: {FileSize} bytes", file.FileName, file.Length);
            
            if (file == null || file.Length == 0)
            {
                logger.LogWarning("Upload rejected: File is empty or not provided");
                return BadRequest("File is empty or not provided");
            }

            if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Upload rejected: Non-txt file: {FileName}", file.FileName);
                return BadRequest("Only .txt files are supported");
            }            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            logger.LogInformation("File {FileName} loaded into memory", file.FileName);

            memoryStream.Position = 0;
            var hash = Convert.ToBase64String(SHA256.Create().ComputeHash(memoryStream));
            memoryStream.Position = 0;
            hash = hash.Replace('/', '-').Replace('+', '-').Replace('=', '-');
            logger.LogInformation("Calculated hash for file {FileName}: {Hash}", file.FileName, hash);

            var existingFile = await fileDBStorageService.GetFileByHashAsync(hash);
            if (existingFile != null)
            {
                logger.LogInformation("Found existing file with the same hash: {Id}", existingFile.Id);
                return Ok(new { existingFile.Id });
            }

            logger.LogInformation("No duplicate found, storing new file with hash: {Hash}", hash);
            var fileModel = new FileModel(hash, file.FileName);
            await fileDBStorageService.SaveFileAsync(fileModel);
            await fileDriveStorageService.StoreFileAsync($"{hash}.txt", memoryStream);
            logger.LogInformation("File successfully stored with ID: {Id}", fileModel.Id);

            return Ok(new { fileModel.Id });
        }        /// <summary>
        /// Retrieves a file by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the file</param>
        /// <returns>The file content</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            logger.LogInformation("File retrieval requested for ID: {Id}", id);
            
            var fileModel = await fileDBStorageService.GetFileByIdAsync(id);
            if (fileModel == null)
            {
                logger.LogWarning("File not found in database: {Id}", id);
                return NotFound();
            }

            logger.LogInformation("File found in database: {Id}, hash: {Hash}, name: {Name}", 
                id, fileModel.Hash, fileModel.Name);
                
            var stream = await fileDriveStorageService.GetFileAsync($"{fileModel.Hash}.txt");
            if (stream == null)
            {
                logger.LogWarning("File content not found on disk for ID: {Id}, hash: {Hash}", id, fileModel.Hash);
                return NotFound();
            }

            logger.LogInformation("File content retrieved successfully for ID: {Id}", id);
            return File(stream, "text/plain", fileModel.Name);
        }
    }
}
