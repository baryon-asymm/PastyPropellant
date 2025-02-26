using Microsoft.AspNetCore.Mvc;
using PastyPropellant.Core.Utils;
using TelegramBot.Service.Shared.Models;

namespace TelegramBot.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotifyController : ControllerBase
{
    private readonly ILogger<NotifyController> _logger;

    public NotifyController(ILogger<NotifyController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Post([FromBody] NotifyRequest request)
    {
        _logger.LogInformation("Received notification request: {0}", request);

        EventBus<NotifyRequest>.Publish(request);

        return Ok();
    }
}
