using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    /// <summary>
    /// Gateway controller for text analysis operations
    /// </summary>
    /// <remarks>
    /// This controller forwards requests to the appropriate microservices
    /// and coordinates responses between them
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class TextAnalysisController(
        ILogger<TextAnalysisController> logger,
        HttpClient httpClient,
        IConfiguration configuration) : ControllerBase
    {
        /// <summary>
        /// Uploads a text file for storage and analysis
        /// </summary>
        /// <param name="file">The text file to upload</param>
        /// <returns>The ID of the uploaded file</returns>
        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            logger.LogInformation("API Gateway: File upload requested: {FileName}, size: {FileSize} bytes", 
                file.FileName, file.Length);
            
            try
            {
                // Forward the file to the File Storage Service
                using var formContent = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                formContent.Add(fileContent, "file", file.FileName);

                // Get service URLs from configuration
                var fileStorageServiceUrl = configuration["ServiceUrls:FileStorageService"];
                logger.LogInformation("Forwarding file to FileStorageService at {ServiceUrl}", fileStorageServiceUrl);                // Call the FileStorageService API
                var fileResponse = await httpClient.PostAsync($"{fileStorageServiceUrl}/api/Files", formContent);
                fileResponse.EnsureSuccessStatusCode();
                logger.LogInformation("FileStorageService responded with status: {StatusCode}", fileResponse.StatusCode);

                // Parse the response to get the file ID
                var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                var fileResult = System.Text.Json.JsonDocument.Parse(fileResponseContent).RootElement;
                Guid id = fileResult.GetProperty("id").GetGuid();
                logger.LogInformation("File successfully stored with ID: {Id}", id);

                return Ok(new { id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing file {FileName}: {ErrorMessage}", file.FileName, ex.Message);
                return StatusCode(500, "An error occurred while processing the file");
            }
        }
        /// <summary>
        /// Downloads a file by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the file</param>
        /// <returns>The file content</returns>
        [HttpGet("download-file/{id}")]
        public async Task<IActionResult> DownloadFile(Guid id)
        {
            logger.LogInformation("API Gateway: File download requested for ID: {Id}", id);
            
            try
            {
                // Get service URLs from configuration
                var fileStorageServiceUrl = configuration["ServiceUrls:FileStorageService"];
                logger.LogInformation("Requesting file from FileStorageService at {ServiceUrl}", fileStorageServiceUrl);

                // Call the FileStorageService API
                var response = await httpClient.GetAsync($"{fileStorageServiceUrl}/api/Files/{id}");
                response.EnsureSuccessStatusCode();
                logger.LogInformation("FileStorageService responded with status: {StatusCode}", response.StatusCode);                // Get the file content and content type
                var fileStream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";
                var fileName = response.Content.Headers.ContentDisposition?.FileName ?? $"file-{id}";

                // Remove quotes if they exist in the filename
                fileName = fileName.Trim('"');
                logger.LogInformation("Retrieved file: {FileName}, content type: {ContentType}", fileName, contentType);

                // Return the file to the client
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving file for ID {Id}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "An error occurred while retrieving the file");
            }
        }
        /// <summary>
        /// Retrieves text analysis results for a file by ID
        /// </summary>
        /// <param name="id">The unique identifier of the file</param>
        /// <returns>Analysis results including paragraph, word, and character count</returns>
        [HttpGet("analysis-file/{id}")]
        public async Task<IActionResult> GetAnalysisFile(Guid id)
        {
            logger.LogInformation("API Gateway: File analysis requested for ID: {Id}", id);
            
            try
            {
                // Get service URLs from configuration
                var fileAnalysisServiceUrl = configuration["ServiceUrls:FileAnalysisService"];
                logger.LogInformation("Requesting analysis from FileAnalysisService at {ServiceUrl}", fileAnalysisServiceUrl);

                // Call the FileAnalysisService API to get analysis results
                var response = await httpClient.GetAsync($"{fileAnalysisServiceUrl}/api/Analysis/analysis-file/{id}");
                response.EnsureSuccessStatusCode();
                logger.LogInformation("FileAnalysisService responded with status: {StatusCode}", response.StatusCode);                var responseContent = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonDocument.Parse(responseContent).RootElement;
                int paragraphCount = result.GetProperty("paragraphCount").GetInt32();
                int wordCount = result.GetProperty("wordCount").GetInt32();
                int characterCount = result.GetProperty("characterCount").GetInt32();
                result.TryGetProperty("wordCloudAvailable", out var wordCloudProp);
                bool wordCloudAvailable = wordCloudProp.GetBoolean();
                
                logger.LogInformation("Analysis results retrieved for file {Id}: {ParagraphCount} paragraphs, {WordCount} words, {CharacterCount} characters, wordCloud: {WordCloudAvailable}", 
                    id, paragraphCount, wordCount, characterCount, wordCloudAvailable);

                return Ok(new
                {
                    paragraphCount,
                    wordCount,
                    characterCount,
                    wordCloudAvailable,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving analysis results for ID {Id}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "An error occurred while retrieving analysis results");
            }
        }
        /// <summary>
        /// Retrieves the word cloud image for a file by ID
        /// </summary>
        /// <param name="id">The unique identifier of the file</param>
        /// <returns>Word cloud image in SVG format</returns>
        [HttpGet("word-cloud/{id}")]
        public async Task<IActionResult> GetWordCloud(Guid id)
        {
            logger.LogInformation("API Gateway: Word cloud requested for ID: {Id}", id);
            
            try
            {
                // Get service URLs from configuration
                var fileAnalysisServiceUrl = configuration["ServiceUrls:FileAnalysisService"];
                logger.LogInformation("Requesting word cloud from FileAnalysisService at {ServiceUrl}", fileAnalysisServiceUrl);

                // Call the FileAnalysisService API to get the word cloud image
                var response = await httpClient.GetAsync($"{fileAnalysisServiceUrl}/api/Analysis/word-cloud/{id}");
                response.EnsureSuccessStatusCode();
                logger.LogInformation("FileAnalysisService responded with status: {StatusCode}", response.StatusCode);

                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/svg+xml";
                var fileName = $"wordcloud-{id}.svg";
                logger.LogInformation("Word cloud retrieved for file {Id}, content type: {ContentType}", id, contentType);

                // Return the word cloud image to the client
                return File(await response.Content.ReadAsStreamAsync(), contentType, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving word cloud for ID {Id}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "An error occurred while retrieving the word cloud");
            }
        }
    }
}
