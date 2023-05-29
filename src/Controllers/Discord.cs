using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Extensions;
using Remora.Discord.API.Objects;

namespace OoLunar.PollMaster3000.Controllers
{
    [ApiController, Route("api/discord")]
    public sealed class DiscordController : ControllerBase
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions;

        static DiscordController() => _jsonSerializerOptions = new ServiceCollection()
            .ConfigureDiscordJsonConverters()
            .BuildServiceProvider()
            .GetRequiredService<IOptionsSnapshot<JsonSerializerOptions>>()
            .Get("Discord");

        [HttpPost]
        public IActionResult PostAsync()
        {
            Interaction? interaction = JsonSerializer.Deserialize<Interaction>(HttpContext.Request.Body, _jsonSerializerOptions);
            if (interaction is null)
            {
                return BadRequest();
            }

            return Ok(JsonSerializer.Serialize(new InteractionResponse(InteractionCallbackType.Pong), _jsonSerializerOptions));
        }
    }
}
