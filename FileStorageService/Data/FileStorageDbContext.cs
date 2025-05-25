using FileStorageService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Data
{
    public class FileStorageDbContext : DbContext
    {
        public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options) : base(options) { }

        public DbSet<FileModel> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FileModel>()
                .HasKey(f => f.Id);


            modelBuilder.Entity<FileModel>()
                .HasIndex(f => f.Hash)
                .IsUnique();
        }
    }
}
