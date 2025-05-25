namespace FileAnalysisService.Models
{
    public class FileAnalysisModel
    {
        public Guid FileId { get; set; }
        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int CharacterCount { get; set; }
        public string WordCloudLocation { get; set; } = string.Empty;
    }
}
