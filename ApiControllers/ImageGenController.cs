using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/image/")]
public class ImageGenController : ControllerBase
{
    [HttpPost("on-tune-creation")]
    public async Task<IActionResult> OnTuneCreation([FromQuery] Guid userId, [FromQuery] Guid tuneId)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var requestBody = await reader.ReadToEndAsync();
        
        Console.WriteLine("---------------------------------------");
        Console.WriteLine(requestBody);
        Console.WriteLine("---------------------------------------");
        
        return Ok("Server has received the request");
    }
}
