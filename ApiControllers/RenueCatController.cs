using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAiBackend.Models;
using PhotoAiBackend.Persistance;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/revenuecat/")]
public class RenueCatController : ControllerBase
{
    private readonly AppDbContext _context;

    public RenueCatController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpPost("on-subscription-event")]
    public async Task<IActionResult> OnSubscriptionEvent()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var requestBody = JsonSerializer.Deserialize<RevenueCatWebhookPayload>(body);

        if (requestBody == null)
        {
            return BadRequest();
        }
        
        var id = Guid.Parse(requestBody.Event.AppUserId);
        var foundUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (foundUser == null)
        {
            return NotFound("User not found");
        }

        Console.WriteLine(requestBody.Event.ProductId);
        Console.WriteLine(requestBody.Event.Type);
        
        return Ok();
    }
}