using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAiBackend.Models;
using PhotoAiBackend.Persistance;
using PhotoAiBackend.Persistance.Entities;
using PhotoAiBackend.Services;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/image/")]
public class ImageGenController : ControllerBase
{
    private readonly string _apiKey;
    private AppDbContext _dbContext;
    private INotificationService _notificationService;

    public ImageGenController(AppDbContext dbContext, INotificationService notificationService)
    {
        _apiKey = Environment.GetEnvironmentVariable("AstriaAiApiKey");
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    [HttpPost("generate-headshot")]
    public async Task<IActionResult> GenerateHeadshot([FromQuery] Guid userId, [FromQuery] string prompt, [FromQuery] string presetCategory)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return BadRequest("User not found");
        }
        
        var apiUrl = $"https://api.astria.ai/tunes/1504944/prompts";

        var values = new Dictionary<string, string>
        {
            {"prompt[text]", $"<lora:{user.TuneId}:1> {prompt}"},
            {"prompt[callback]", $"https://image-generation-backend-164860087792.us-central1.run.app/api/image/on-image-generated?userId={user.Id}"}
        };

        var content = new FormUrlEncodedContent(values);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await httpClient.PostAsync(apiUrl, content);

        var responseBody = await response.Content.ReadAsStringAsync();
        
        try
        {
            var jobInfo = JsonSerializer.Deserialize<ImageGenerationResponse>(responseBody);

            var job = new ImageJob
            {
                Id = jobInfo.Id,
                CreationDate = jobInfo.CreatedAt,
                Status = JobStatus.Processing,
                SystemPrompt = jobInfo.Text,
                UserId = userId,
                Images = "[]",
                PresetCategory = Enum.TryParse<PresetCategory>(presetCategory, true, out var parsedCategory)
                    ? parsedCategory
                    : PresetCategory.Headshots
            };

            await _dbContext.ImageJobs.AddAsync(job);
            await _dbContext.SaveChangesAsync();

            return Ok(job);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("on-image-generated")]
    public async Task<IActionResult> OnImageGenerated([FromQuery] Guid userId)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var requestBody = JsonSerializer.Deserialize<ImageGenerationCallbackPayload>(body);

        var foundUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (requestBody == null)
        {
            return BadRequest("The request callback body is invalid.");
        }

        if (foundUser == null)
        {
            return BadRequest("The user couldn't be found.");
        }

        var foundJob = await _dbContext.ImageJobs.FirstOrDefaultAsync(ij => ij.Id == requestBody.Prompt.Id);

        if (foundJob == null)
        {
            return BadRequest("The job couldn't be found.");
        }
        
        foundJob.Images = JsonSerializer.Serialize(requestBody.Prompt.Images);
        foundJob.Status = JobStatus.Done;

        foundUser.Credits -= 15;

        await _dbContext.SaveChangesAsync();

        var data = new Dictionary<string, string>
        {
            { "type", GenerationType.Headshot.ToString() },
            { "jobId", foundJob.Id.ToString() }
        };

        IReadOnlyDictionary<string, string> readOnlyData = new ReadOnlyDictionary<string, string>(data);
        
        var notification = new NotificationInfo
        {
            Title = "Headshot Ready!",
            Text = "Your AI-generated headshot is complete. Tap to view your results."
        };
        
        if (foundUser.FcmTokenId != null)
        {
            await _notificationService.SendNotificatino(foundUser.FcmTokenId, notification, readOnlyData);
        }
        
        return Ok();
    }

    [HttpPost("tune-model")]
    public async Task<IActionResult> TuneModel([FromForm] List<IFormFile> images, [FromQuery] string gender)
    {
        if (!images.Any())
        {
            return BadRequest("No images uploaded.");
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var content = new MultipartFormDataContent();

        content.Add(new StringContent(Guid.NewGuid().ToString()), "tune[title]");
        content.Add(new StringContent(gender), "tune[name]");
        content.Add(new StringContent("flux-lora-portrait"), "tune[preset]");
        content.Add(new StringContent("1504944"), "tune[base_tune_id]");
        content.Add(new StringContent("lora"), "tune[model_type]");

        foreach (var image in images)
        {
            var streamContent = new StreamContent(image.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            content.Add(streamContent, "tune[images][]", image.FileName);
        }

        var response = await httpClient.PostAsync("https://api.astria.ai/tunes", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }

        var result = await response.Content.ReadAsStringAsync();
        return Ok(result);
    }
}
