using Microsoft.AspNetCore.Mvc;

namespace CatalogOrderApi.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    // GET: api/health
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "CatalogOrderApi"
        });
    }
}
