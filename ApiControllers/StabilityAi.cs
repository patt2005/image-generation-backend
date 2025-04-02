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
        var boundary = "Boundary-" + Guid.NewGuid();
        var content = new MultipartFormDataContent(boundary);

        var imageContent = new StreamContent(image.OpenReadStream());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "image/jpeg");

        imageContent.Headers.TryAddWithoutValidation("Content-Disposition", $"form-data; name=image; filename={image.FileName}");
        content.Add(imageContent, "image");

        var formatContent = new StringContent(outputFormat);
        formatContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=output_format");
        content.Add(formatContent, "output_format");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        request.Content = content;

        using var client = new HttpClient();
        var response = await client.SendAsync(request);

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