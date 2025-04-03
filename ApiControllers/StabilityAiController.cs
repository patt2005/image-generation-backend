using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/stability/")]
public class StabilityAiController : ControllerBase
{
    private readonly string _apiKey;
    private readonly string _apiUrl = "https://api.stability.ai/v2beta/stable-image/upscale/fast";

    public StabilityAiController()
    {
        _apiKey = Environment.GetEnvironmentVariable("StabilityAiApiKey");
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
    public async Task<IActionResult> RemoveBackground([FromForm] IFormFile image, [FromQuery] string outputFormat = "png")
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
        
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
        
            return File(imageBytes, contentType);
        }
    }
}