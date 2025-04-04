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

    [HttpGet("list-job-images")]
    public async Task<IActionResult> GetJobImages([FromQuery] string jobId)
    {
        var images = await _dbContext.EnhanceImages.Where(i => i.JobId == jobId).ToListAsync();
        
        return Ok(images);
    }
    
    [HttpGet("list-enhance-jobs")]
    public async Task<IActionResult> GetEnhanceJobs([FromQuery] Guid userId)
    {
        var foundUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (foundUser == null)
        {
            return NotFound();
        }
        
        var jobs = _dbContext.EnhanceJobs
            .Where(j => j.UserId == userId);
        
        return Ok(jobs);
    }
}