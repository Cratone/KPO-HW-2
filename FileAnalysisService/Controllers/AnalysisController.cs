using System.Text;
using FileAnalysisService.Models;
using FileAnalysisService.Services;
using FileStorageService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Controllers
{
    /// <summary>
    /// Controller for text file analysis operations
    /// </summary>
    /// <remarks>
    /// This controller handles requests related to file analysis,
    /// including text statistics and word cloud generation
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController (
        FileAnalysisDbContext dbContext,
        TextAnalysisService textAnalysisService,
        ILogger<AnalysisController> logger,
        HttpClient httpClient,
        IConfiguration configuration) : ControllerBase
    {        /// <summary>
        /// Retrieves text analysis for a file by ID
        /// </summary>
        /// <param name="id">The unique identifier of the file</param>
        /// <returns>Analysis results including paragraph, word, and character count</returns>
        [HttpGet("analysis-file/{id}")]
        public async Task<IActionResult> GetAnalysisFile(Guid id)
        {
            logger.LogInformation("Requested analysis for file with ID: {FileId}", id);
            
            // Check if the file exists in the database
            var fileAnalysis = await dbContext.Set<FileAnalysisModel>().FirstOrDefaultAsync(f => f.FileId == id);
            if (fileAnalysis != null)
            {
                logger.LogInformation("Found existing analysis for file {FileId}", id);
                bool wordCloudAvailable = !string.IsNullOrEmpty(fileAnalysis.WordCloudLocation);
                return Ok(new
                {
                    fileAnalysis.ParagraphCount,
                    fileAnalysis.WordCount,
                    fileAnalysis.CharacterCount,
                    wordCloudAvailable
                });
            }
            try
            {
                // Get the FileStorageService URL from configuration
                var fileStorageServiceUrl = configuration["ServiceUrls:FileStorageService"];
                logger.LogInformation("Fetching file content from FileStorageService at URL: {ServiceUrl}", fileStorageServiceUrl);

                // Fetch the file from FileStorageService
                var fileResponse = await httpClient.GetAsync($"{fileStorageServiceUrl}/api/Files/{id}");
                fileResponse.EnsureSuccessStatusCode();
                var fileContent = await fileResponse.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully retrieved file content for {FileId}, content length: {ContentLength}", id, fileContent.Length);

                var result = await textAnalysisService.AnalyzeTextAsync(id, fileContent);
                bool wordCloudAvailable = !string.IsNullOrEmpty(result.WordCloudLocation);


                logger.LogInformation("File with id {FileId} analyzed", id);                // Save the analysis result to the database
                await dbContext.Set<FileAnalysisModel>().AddAsync(result);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Analysis results saved to database for file {FileId}", id);

                return Ok(new
                {
                    result.ParagraphCount,
                    result.WordCount,
                    result.CharacterCount,
                    wordCloudAvailable
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing file {FileId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, "Error analyzing file");
            }
        }        /// <summary>
        /// Retrieves the word cloud image for a file by ID
        /// </summary>
        /// <param name="id">The unique identifier of the file</param>
        /// <returns>Word cloud image in SVG format</returns>
        [HttpGet("word-cloud/{id}")]
        public IActionResult GetWordCloud(Guid id)
        {
            logger.LogInformation("Requested word cloud for file with ID: {FileId}", id);
            
            // Get the analysis result from the database
            var fileAnalysis = dbContext.Set<FileAnalysisModel>().FirstOrDefault(f => f.FileId == id);
            if (fileAnalysis == null || string.IsNullOrEmpty(fileAnalysis.WordCloudLocation))
            {
                logger.LogWarning("Word cloud not found for file {FileId}", id);
                return NotFound("Word cloud not found for this file");
            }

            // Return the word cloud image
            var wordCloudPath = fileAnalysis.WordCloudLocation;
            if (!System.IO.File.Exists(wordCloudPath))
            {
                logger.LogWarning("Word cloud image file not found at path: {Path}", wordCloudPath);
                return NotFound("Word cloud image not found");
            }

            logger.LogInformation("Returning word cloud image for file {FileId}", id);
            var fileBytes = System.IO.File.ReadAllBytes(wordCloudPath);
            return File(fileBytes, "image/svg+xml", $"{id}.svg");
        }
    }
}
