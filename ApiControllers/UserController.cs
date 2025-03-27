using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PhotoAiBackend.Models;
using PhotoAiBackend.Persistance;
using PhotoAiBackend.Persistance.Entities;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/user/")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    
    public UserController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("test")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _dbContext.Users.ToListAsync();
        return Ok(users);
    }
    
    [HttpPost("register-user")]
    public async Task<IActionResult> RegisterUser()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var requestBody = await reader.ReadToEndAsync();
            
            var payload = JsonSerializer.Deserialize<RegisterUserPayload>(requestBody);

            if (payload == null)
            {
                return BadRequest("Invalid request body");
            }
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                FcmTokenId = payload.FcmTokenId,
                Gender = payload.Gender,
                TuneId = payload.TuneId,
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var response = new
            {
                userId = user.Id
            };
            
            return Ok(response);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}