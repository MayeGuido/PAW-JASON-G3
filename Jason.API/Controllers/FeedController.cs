using Jason.API.Services;
using Jason.Models.Exchange;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

// Entrega el feed: las fichas de todas las fuentes, ya convertidas al formato común.
// El controller solo recibe la petición y delega el trabajo en IFeedService.
[ApiController]
[Route("api/feed")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    // Público: una fuente caída no impide que se devuelvan las demás.
    [HttpGet]
    public async Task<ActionResult<DashboardExport>> Get(CancellationToken cancellationToken)
    {
        DashboardExport feed = await _feedService.GetAsync(cancellationToken);
        return Ok(feed);
    }
}
