using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/stability/")]
public class StabilityAi : ControllerBase
{
    private readonly string _apiKey;
    private readonly string _apiUrl = "https://api.stability.ai/v2beta/stable-image/upscale/fast";

    public StabilityAi()
    {
        _apiKey = Environment.GetEnvironmentVariable("StabilityAiApiKey");
    }
    
    [HttpPost("enhance")]
    public async Task<IActionResult> Enhance([FromForm] IFormFile image, [FromQuery] string outputFormat = "png")
    {
        if (image == null || image.Length == 0)
            return BadRequest("No image uploaded.");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stability.ai/v2beta/stable-image/upscale/fast");

        var content = new MultipartFormDataContent(); // Do not set boundary manually!

        // Required image field
        var imageContent = new StreamContent(image.OpenReadStream());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "image/jpeg");
        imageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "image",        
            FileName = image.FileName
        };
        content.Add(imageContent, "image");
        
        var formatContent = new StringContent(outputFormat);
        content.Add(formatContent, "output_format");
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        
        request.Headers.Add("stability-client-id", "face-ai-app");
        request.Headers.Add("stability-client-version", "1.0.0");
        request.Headers.Add("stability-client-user-id", "user@codbun");

        request.Content = content;

        using var client = new HttpClient();
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }

        var imageData = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

        return File(imageData, contentType);
    }
}