using FileAnalysisService.Models;

namespace FileAnalysisService.Services
{
    /// <summary>
    /// Service for analyzing text content and generating word clouds
    /// </summary>
    public class TextAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TextAnalysisService> _logger;

        /// <summary>
        /// Initializes a new instance of the TextAnalysisService
        /// </summary>
        /// <param name="httpClient">HTTP client for external API calls</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger for the service</param>
        public TextAnalysisService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<TextAnalysisService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }        public async Task<FileAnalysisModel> AnalyzeTextAsync(Guid fileId, string fileContent)
        {
            _logger.LogInformation("Starting text analysis for file {FileId}, content length: {ContentLength}", fileId, fileContent.Length);
            
            var result = new FileAnalysisModel
            {
                FileId = fileId
            };

            // Calculate paragraph count (split by double newlines)
            result.ParagraphCount = fileContent.Split("\n", StringSplitOptions.RemoveEmptyEntries).Length;

            // Calculate word count
            result.WordCount = fileContent.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Calculate character count
            result.CharacterCount = fileContent.Length;
            
            _logger.LogInformation("Text statistics calculated for file {FileId}: {ParagraphCount} paragraphs, {WordCount} words, {CharacterCount} characters", 
                fileId, result.ParagraphCount, result.WordCount, result.CharacterCount);            try
            {
                // Generate word cloud URL using QuickChart.io
                _logger.LogInformation("Generating word cloud using QuickChart.io API for file {FileId}", fileId);
                var fileResponse = await _httpClient.GetAsync($"https://quickchart.io/wordcloud?text={Uri.EscapeDataString(fileContent)}");
                fileResponse.EnsureSuccessStatusCode();
                
                // Get the content type from the response headers
                var contentType = fileResponse.Content.Headers.ContentType?.MediaType;
                _logger.LogInformation("Word cloud response received with content type: {ContentType}", contentType);

                // Read the image data
                var imageBytes = await fileResponse.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Word cloud image received, size: {ImageSize} bytes", imageBytes.Length);

                // Save the word cloud image to the specified directory
                var storagePath = _configuration["WordCloudStorage:Path"];
                _logger.LogInformation("Saving word cloud to storage path: {StoragePath}", storagePath);
                
                if (string.IsNullOrEmpty(storagePath))
                {
                    _logger.LogWarning("WordCloudStorage:Path configuration is missing or empty");
                    storagePath = "WordCloudStorage"; // Fallback path
                }
                
                Directory.CreateDirectory(storagePath); // Ensure directory exists
                var wordCloudPath = Path.Combine(storagePath, $"{fileId}.svg");
                await File.WriteAllBytesAsync(wordCloudPath, imageBytes);
                _logger.LogInformation("Word cloud saved to: {WordCloudPath}", wordCloudPath);

                // Store the path in the result
                result.WordCloudLocation = wordCloudPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate or save word cloud for file {FileId}: {ErrorMessage}", fileId, ex.Message);
                result.WordCloudLocation = "";            }
            
            _logger.LogInformation("Text analysis completed for file {FileId}", fileId);
            return result;
        }
    }
}
