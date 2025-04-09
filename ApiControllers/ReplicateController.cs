using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAiBackend.Models;
using PhotoAiBackend.Persistance;
using PhotoAiBackend.Persistance.Entities;
using PhotoAiBackend.Services;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/replicate/")]
public class ReplicateController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly string _apiKey;
    private INotificationService _notificationService;

    public ReplicateController(AppDbContext dbContext, INotificationService notificationService)
    {
        _apiKey = Environment.GetEnvironmentVariable("ReplicateApiKey");
        _dbContext = dbContext;
        _notificationService = notificationService;
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
    
    private async Task<byte[]> DownloadImageAsync(string imageUrl)
    {
        using var httpClient = new HttpClient();
    
        var response = await httpClient.GetAsync(imageUrl);
    
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch image. Status: {response.StatusCode}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    [HttpPost("on-prediction-complete")]
    public async Task<IActionResult> OnPredictionComplete([FromQuery] Guid userId)
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        Console.WriteLine("-------------------------------");
        Console.WriteLine(body);
        Console.WriteLine("-------------------------------");
        
        var result = JsonSerializer.Deserialize<EnhanceCallbackPayload>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
        {
            return BadRequest("Failed to deserialize payload.");
        }
        
        var foundJob = await _dbContext.EnhanceJobs.FirstOrDefaultAsync(j => j.Id == result.Id);

        if (result.Status != "succeeded")
        {
            foundJob.Status = Enum.TryParse(result.Status, out EnhanceStatus status) ? status : EnhanceStatus.Running;
            
            return Ok("Status changed to " + result.Status);
        }
        
        var foundUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (foundUser == null)
        {
            return NotFound("User not found.");
        }
        
        if (result?.Output == null)
        {
            return BadRequest("Failed to parse response.");
        }

        if (foundJob == null)
        {
            return NotFound("Job not found.");
        }

        try
        {
            var data = await DownloadImageAsync(result.Input.Img);

            var image = new EnhanceImage
            {
                Id = Guid.NewGuid(),
                JobId = foundJob.Id,
                Data = data
            };

            await _dbContext.EnhanceImages.AddAsync(image);
            foundJob.Status = EnhanceStatus.Succeeded;

            foundUser.Credits -= 10;
            
            await _dbContext.SaveChangesAsync();
        
            if (foundUser.FcmTokenId != null)
            {
                var notificationData = new Dictionary<string, string>
                {
                    { "type", GenerationType.Filter.ToString() },
                    { "jobId", foundJob.Id }
                };

                IReadOnlyDictionary<string, string> readOnlyData = new ReadOnlyDictionary<string, string>(notificationData);
        
                var notification = new NotificationInfo
                {
                    Title = "Photo Enhanced!",
                    Text = "Your image has been enhanced with AI. Tap to see the improved version."
                };  
                
                await _notificationService.SendNotificatino(foundUser.FcmTokenId, notification, readOnlyData);
            }
        
            return Ok("The message was received.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    
    [HttpPost("create-prediction")]
    public async Task<IActionResult> CreatePrediction([FromQuery] string imageUrl, [FromQuery] Guid userId)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return BadRequest("Missing image URL.");
        }

        try
        {
            var replicateApiUrl = "https://api.replicate.com/v1/predictions";
            var webhookUrl = $"https://image-generation-backend-164860087792.us-central1.run.app/api/replicate/on-prediction-complete?userId={userId}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var payload = new
            {
                version = "0fbacf7afc6c144e5be9767cff80f25aff23e52b0708f17e20f9879b2f21516c",
                webhook = webhookUrl,
                webhook_events_filter = new[] { "completed" },
                input = new
                {
                    image = imageUrl,
                    scale = 2
                }
            };
            
            var json = JsonSerializer.Serialize(payload);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(replicateApiUrl, content);
            
            if (!response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.Created)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"❌ Request failed: {error}");
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<EnhanceCallbackPayload>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                return BadRequest("❌ Failed to parse response body.");
            }
            
            var enhanceJob = new EnhanceJob
            {
                Id = result.Id,
                Status = EnhanceStatus.Running,
                CreatedAt = result.CreatedAt,
                UserId = userId,
                EnhanceImages = []
            };
            
            await _dbContext.EnhanceJobs.AddAsync(enhanceJob);
            await _dbContext.SaveChangesAsync();

            return Ok(enhanceJob);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"❌ Error: {ex.Message}");
        }
    }
}