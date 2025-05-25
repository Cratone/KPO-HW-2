namespace FileStorageService.Models
{
    public class FileModel
    {
        public Guid Id { get; } = new Guid();

        public string Hash { get; set; } = null!;

        public string Name { get; set; } = null!;

        public FileModel(string hash, string name)
        {
            Hash = hash;
            Name = name;
        }
    }
}