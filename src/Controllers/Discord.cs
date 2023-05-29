using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace OoLunar.PollMaster3000.Controllers
{
    [ApiController, Route("api/discord")]
    public sealed class DiscordController : ControllerBase
    {
        public static JsonSerializerOptions DiscordJsonSerializerOptions { get; internal set; } = new();
        private readonly ILogger<DiscordController> _logger;

        public DiscordController(ILogger<DiscordController> logger) => _logger = logger;

        [HttpPost]
        public IActionResult PostAsync()
        {
            Interaction? interaction = JsonSerializer.Deserialize<Interaction>(HttpContext.Request.Body, DiscordJsonSerializerOptions);
            if (interaction is null)
            {
                return BadRequest();
            }

            return Ok(JsonSerializer.Serialize(new InteractionResponse(InteractionCallbackType.Pong), DiscordJsonSerializerOptions));
        }
    }
}
