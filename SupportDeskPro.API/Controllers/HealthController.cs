using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public HealthController(IApplicationDbContext db)
    {
        _db = db;
    }
      
    /// <summary>
    /// Health check endpoint — keeps Azure SQL awake.
    /// Pinged every 5 minutes by UptimeRobot.
    /// because using Free Limit Azure SQL database 
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        try
        {
            // Ping DB to keep it awake 
            var canConnect = await _db.Tenants.AnyAsync();
            return Ok(new
            {
                status = "healthy",
                database = canConnect ? "connected" : "empty",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            // Return 200 even if DB is waking up
            // So UptimeRobot doesn't think the API is down 
            return Ok(new {
                status = "waking_up",
                database = "connecting",
                timestamp = DateTime.UtcNow
            });
        }

    }
}