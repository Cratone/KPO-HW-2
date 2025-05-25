using FileStorageService.Data;
using FileStorageService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FileStorageService.Services
{
    public class FileDBStorageService
    {
        private readonly FileStorageDbContext _dbContext;
        private readonly ILogger<FileDBStorageService> _logger;

        public FileDBStorageService(
            FileStorageDbContext dbContext,
            ILogger<FileDBStorageService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<FileModel?> GetFileByHashAsync(string hash)
        {
            return await _dbContext.Files.FirstOrDefaultAsync(f => f.Hash == hash);
        }

        public async Task<FileModel?> GetFileByIdAsync(Guid id)
        {
            return await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task SaveFileAsync(FileModel file)
        {
            await _dbContext.Files.AddAsync(file);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Saved file with ID {Id} and hash {Hash} to database", file.Id, file.Hash);
        }
    }
}
