using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAiBackend.Models;
using PhotoAiBackend.Persistance;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/revenuecat/")]
public class RevenueCatController : ControllerBase
{
    private readonly AppDbContext _context;

    public RevenueCatController(AppDbContext context)
    {
        _context = context;
    }
    
    // [HttpPost("add-credits-after-purchase")]
    // public async Task<IActionResult> AddCreditsAfterPurchase([FromQuery] Guid userId)
    
    [HttpPost("on-subscription-event")]
    public async Task<IActionResult> OnSubscriptionEvent()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var requestBody = JsonSerializer.Deserialize<RevenueCatWebhookPayload>(body);
        
        if (requestBody == null)
        {
            return BadRequest("Invalid payload");
        }
        
        if (!Guid.TryParse(requestBody.Event.AppUserId, out Guid userId))
        {
            return BadRequest("Invalid user ID format");
        }

        var foundUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (foundUser == null)
        {
            return NotFound("User not found");
        }
        
        var productId = requestBody.Event.ProductId;

        if (string.IsNullOrWhiteSpace(productId))
        {
            return BadRequest("Product ID is empty");
        }
        
        switch (productId)
        {
            case "com.face.ai.weekly":
                foundUser.Credits += 200;
                break;
            case "com.face.ai.monthly":
                foundUser.Credits += 1000;
                break;
            default:
                return Ok("Product ID not tracked for credits.");
        }

        await _context.SaveChangesAsync();
        return Ok("Credits updated successfully");
    }
}