using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAiBackend.Models;
using PhotoAiBackend.Persistance;
using PhotoAiBackend.Persistance.Entities;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/replicate/")]
public class ReplicateController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly string _apiKey;

    public ReplicateController(AppDbContext dbContext)
    {
        _apiKey = Environment.GetEnvironmentVariable("ReplicateApiKey");
        _dbContext = dbContext;
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        return Ok(_apiKey);
    }
    
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided.");
            }
    
            var replicateUrl = "https://api.replicate.com/v1/files";
            using var httpClient = new HttpClient();
    
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            var content = new MultipartFormDataContent();
    
            var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            
            content.Add(fileContent, "\"content\"", "\"filename.jpg\"");
    
            var response = await httpClient.PostAsync(replicateUrl, content);
    
            var responseText = await response.Content.ReadAsStringAsync();
    
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, responseText);
            }
            
            var json = JsonDocument.Parse(responseText);
            var url = json.RootElement.GetProperty("urls").GetProperty("get").GetString();
    
            return Ok(new { imageUrl = url });
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error uploading image to Replicate:", ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("on-prediction-complete")]
    public async Task<IActionResult> OnPredictionComplete([FromQuery] Guid userId)
    {
        var foundUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (foundUser == null)
        {
            return NotFound("User not found.");
        }
        
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        var result = JsonSerializer.Deserialize<EnhanceCallbackPayload>(body);

        if (result == null)
        {
            return BadRequest("Failed to parse response.");
        }

        var enhanceJob = new EnhanceJob
        {
            Id = result.Id,
            Status = Enum.TryParse<EnhanceStatus>(result.Status, true, out var parsedStatus)
                ? parsedStatus
                : EnhanceStatus.Failed,
            CreatedAt = result.CreatedAt,
            Output = JsonSerializer.Serialize(result.Output),
            UserId = userId
        };
        
        await _dbContext.EnhanceJobs.AddAsync(enhanceJob);
        await _dbContext.SaveChangesAsync();
        
        return Ok("The message was received.");
    }
    
    [HttpPost("create-prediction")]
    public async Task<IActionResult> CreatePrediction([FromQuery] string imageUrl, [FromQuery] Guid userId)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("Missing image URL.");

        try
        {
            var replicateApiUrl = "https://api.replicate.com/v1/predictions";
            var webhookUrl = $"https://image-generation-backend-164860087792.us-central1.run.app/on-prediction-complete?userId={userId}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var payload = new
            {
                version = "51ed1464d8bbbaca811153b051d3b09ab42f0bdeb85804ae26ba323d7a66a4ac",
                webhook = webhookUrl,
                webhook_events_filter = new[] { "completed" },
                input = new
                {
                    input = imageUrl,
                    background_upsampler = "DiffBIR",
                    upscaling_model_type = "faces",
                    super_resolution_factor = 2
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(replicateApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, responseBody);

            return Ok(JsonDocument.Parse(responseBody));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"❌ Error: {ex.Message}");
        }
    }
}