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

        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        
        var content = new MultipartFormDataContent();
        
        var imageContent = new StreamContent(image.OpenReadStream());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        content.Add(imageContent, "image", image.FileName);
        
        content.Add(new StringContent(outputFormat), "output_format");
        
        var response = await httpClient.PostAsync("https://api.stability.ai/v2beta/stable-image/upscale/fast", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/webp";
        var responseImage = await response.Content.ReadAsByteArrayAsync();

        return File(responseImage, contentType);
    }
}