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

    [HttpPost("remove-background")]
    public async Task<IActionResult> RemoveBackground()
    {
        return Ok();
    }
}