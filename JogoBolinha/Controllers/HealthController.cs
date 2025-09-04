using Microsoft.AspNetCore.Mvc;

namespace JogoBolinha.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [Route("")]
    [Route("ping")]
    public IActionResult Ping()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    [HttpGet("error")]
    public IActionResult TestError()
    {
        throw new Exception("Test exception for debugging");
    }
}