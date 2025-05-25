using FileStorageService.Data;
using FileStorageService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FileStorageService.Services
{
    public class FileDriveStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileDriveStorageService> _logger;

        public FileDriveStorageService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<FileDriveStorageService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task StoreFileAsync(string fileName, Stream fileStream)
        {
            string storagePath = GetStoragePath();
            Directory.CreateDirectory(storagePath);

            string filePath = Path.Combine(storagePath, fileName);

            using (var fileStream2 = new FileStream(filePath, FileMode.Create))
            {
                fileStream.Position = 0;
                await fileStream.CopyToAsync(fileStream2);
                _logger.LogInformation("Successfully wrote to file with name {FileName}", fileName);
            }
        }

        public async Task<Stream?> GetFileAsync(string fileName)
        {
            string storagePath = GetStoragePath();
            string filePath = Path.Combine(storagePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File content not found at {Path}", filePath);
                return null;
            }

            var stream = new MemoryStream();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(stream);
            }

            stream.Position = 0;
            return stream;
        }

        private string GetStoragePath()
        {
            string basePath = _configuration["FileStorage:Path"] ?? "FileStorage";
            
            if (!Path.IsPathRooted(basePath))
            {
                basePath = Path.Combine(_environment.ContentRootPath, basePath);
            }
            
            return basePath;
        }
    }
}
