using Jason.API.Services;
using Jason.Models.Exchange;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

// In resume, this controller is responsible for providing the feed data to the client
// It uses the IFeedService to retrieve the data and returns it as a DashboardExport object.

// so basically this the structures: the user wannna get the feed -> feedcontroller recives the request -> feedcontroller calls feedservice to get the data -> feedservice returns the data to feedcontroller -> feedcontroller returns the data to the user

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
