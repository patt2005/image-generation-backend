using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using PhotoAiBackend.Persistance;
using PhotoAiBackend.Persistance.Entities;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/stability/")]
public class StabilityAiController : ControllerBase
{
    private readonly string _apiKey;
    private readonly string _apiUrl = "https://api.stability.ai/v2beta/stable-image/upscale/fast";
    private readonly AppDbContext _dbContext;

    public StabilityAiController(AppDbContext dbContext)
    {
        _apiKey = Environment.GetEnvironmentVariable("StabilityAiApiKey");
        _dbContext = dbContext;
    }

    private byte[] GetFileArray(IFormFile file)
    {
        using (var ms = new MemoryStream())
        {
            file.CopyTo(ms);
            return ms.ToArray();
        }
    }
    
    [HttpPost("remove-background")]
    public async Task<IActionResult> RemoveBackground([FromForm] IFormFile image, [FromQuery] Guid userId, string outputFormat = "png")
    {
        if (image == null || image.Length == 0)
            return BadRequest("No image uploaded.");
        
        var stabilityApiUrl = "https://api.stability.ai/v2beta/stable-image/edit/remove-background";
    
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        
            var content = new MultipartFormDataContent();
            
            var imageContent = new ByteArrayContent(GetFileArray(image));
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            content.Add(imageContent, "\"image\"", "\"filename.jpg\"");
        
            if (!string.IsNullOrWhiteSpace(outputFormat))
            {
                content.Add(new StringContent(outputFormat), "\"output_format\"");
            }
        
            var response = await httpClient.PostAsync(stabilityApiUrl, content);
        
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }
            
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            
            var jobId = Guid.NewGuid().ToString();
            var job = new EnhanceJob
            {
                Id = jobId,
                UserId = userId,
                Status = EnhanceStatus.Successful,
                CreatedAt = DateTime.UtcNow
            };
            
            var enhanceImage = new EnhanceImage
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Data = imageBytes
            };
            
            await _dbContext.AddRangeAsync(job, enhanceImage);
            await _dbContext.SaveChangesAsync();
            
            return Ok(job);
        }
    }
}