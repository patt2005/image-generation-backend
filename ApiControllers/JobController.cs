using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAiBackend.Persistance;
using PhotoAiBackend.Persistance.Entities;

namespace PhotoAiBackend.ApiControllers;

[ApiController]
[Route("/api/job/")]
public class JobController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public JobController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [HttpGet("list")]
    public async Task<IActionResult> GetUserImages([FromQuery] Guid userId)
    {
        var foundUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (foundUser == null)
        {
            return NotFound();
        }
        
        var jobs = _dbContext.ImageJobs.Where(j => j.UserId == userId);
        
        return Ok(jobs);
    }

    [HttpPost("mark-as-saved")]
    public async Task<IActionResult> MarkAsSaved([FromQuery] int jobId)
    {
        var foundJob = _dbContext.ImageJobs.FirstOrDefault(j => j.Id == jobId);

        if (foundJob == null)
        {
            return NotFound("Job not found");
        }
        
        foundJob.HasShownPhotos = true;
        await _dbContext.SaveChangesAsync();
        
        return Ok("Saved");
    }
}