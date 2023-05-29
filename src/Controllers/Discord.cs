using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Objects;

namespace OoLunar.PollMaster3000.Controllers
{
    [ApiController, Route("api/discord")]
    public sealed class DiscordController : ControllerBase
    {
        private readonly ILogger<DiscordController> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public DiscordController(ILogger<DiscordController> logger, IOptionsSnapshot<JsonSerializerOptions> jsonSerializerOptions)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(jsonSerializerOptions, nameof(jsonSerializerOptions));

            _logger = logger;
            _jsonSerializerOptions = jsonSerializerOptions.Get("Discord") ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
        }

        [HttpPost]
        public async ValueTask<IActionResult> PostAsync()
        {
            Interaction? interaction = await HttpContext.Request.ReadFromJsonAsync<Interaction>(_jsonSerializerOptions);
            if (interaction is null)
            {
                return BadRequest();
            }

            _logger.LogInformation("Received interaction: {Interaction}", interaction);
            return Ok();
        }
    }
}
