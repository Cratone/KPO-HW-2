using FileAnalysisService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Data
{
    public class FileAnalysisDbContext : DbContext
    {
        public FileAnalysisDbContext(DbContextOptions<FileAnalysisDbContext> options) : base(options) { }

        public DbSet<FileAnalysisModel> Analyzes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FileAnalysisModel>()
                .HasKey(f => f.FileId);
        }
    }
}
