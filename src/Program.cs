using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OoLunar.PollMaster3000.Authentication;
using Remora.Discord.API.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace OoLunar.PollMaster3000
{
    public sealed class Program
    {
        public static Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile(Path.Join(Environment.CurrentDirectory, "res", "config.json"), true, true);
            builder.Configuration.AddJsonFile(Path.Join(Environment.CurrentDirectory, "res", "config.json.prod"), true, true);
            builder.Configuration.AddEnvironmentVariables("POLLMASTER3000_");
            builder.Configuration.AddCommandLine(args);

            string loggingFormat = builder.Configuration.GetValue("Logging:Format", "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u4}] {SourceContext}: {Message:lj}{NewLine}{Exception}") ?? throw new InvalidOperationException("Logging:Format is null");
            string filename = builder.Configuration.GetValue("Logging:Filename", "yyyy'-'MM'-'dd' 'HH'.'mm'.'ss") ?? throw new InvalidOperationException("Logging:Filename is null");

            // Log both to console and the file
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(builder.Configuration.GetValue("Logging:Level", LogEventLevel.Debug))
            .WriteTo.Console(outputTemplate: loggingFormat, theme: new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text] = "\x1b[0m",
                [ConsoleThemeStyle.SecondaryText] = "\x1b[90m",
                [ConsoleThemeStyle.TertiaryText] = "\x1b[90m",
                [ConsoleThemeStyle.Invalid] = "\x1b[31m",
                [ConsoleThemeStyle.Null] = "\x1b[95m",
                [ConsoleThemeStyle.Name] = "\x1b[93m",
                [ConsoleThemeStyle.String] = "\x1b[96m",
                [ConsoleThemeStyle.Number] = "\x1b[95m",
                [ConsoleThemeStyle.Boolean] = "\x1b[95m",
                [ConsoleThemeStyle.Scalar] = "\x1b[95m",
                [ConsoleThemeStyle.LevelVerbose] = "\x1b[34m",
                [ConsoleThemeStyle.LevelDebug] = "\x1b[90m",
                [ConsoleThemeStyle.LevelInformation] = "\x1b[36m",
                [ConsoleThemeStyle.LevelWarning] = "\x1b[33m",
                [ConsoleThemeStyle.LevelError] = "\x1b[31m",
                [ConsoleThemeStyle.LevelFatal] = "\x1b[97;91m"
            }))
            .WriteTo.File(
                $"logs/{DateTime.Now.ToUniversalTime().ToString(filename, CultureInfo.InvariantCulture)}.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: loggingFormat
            );

            // Allow specific namespace log level overrides, which allows us to hush output from things like the database basic SELECT queries on the Information level.
            foreach (IConfigurationSection logOverride in builder.Configuration.GetSection("logging:overrides").GetChildren())
            {
                if (logOverride.Value is null || !Enum.TryParse(logOverride.Value, out LogEventLevel logEventLevel))
                {
                    continue;
                }

                loggerConfiguration.MinimumLevel.Override(logOverride.Key, logEventLevel);
            }

            ILogger logger = loggerConfiguration.CreateLogger();
            builder.Logging.AddSerilog(logger, false);
            builder.Services.AddSerilog(logger, false);
            builder.Services.ConfigureDiscordJsonConverters();
            builder.Services.AddSingleton(services => services.GetRequiredService<IOptionsSnapshot<JsonSerializerOptions>>().Get("Discord"));
            builder.Services.AddAuthenticationCore(options =>
            {
                options.DefaultAuthenticateScheme = "Discord";
                options.DefaultChallengeScheme = "Discord";
                options.AddScheme<DiscordAuthenticationHandler>("Discord", "Discord");
            });

            builder.Services.AddAuthorizationCore(options => options.AddPolicy("Discord", policy => policy.RequireClaim("Discord")));
            builder.Services.AddWebEncoders();
            builder.Services.AddSingleton<ISystemClock, SystemClock>();
            builder.Services.AddControllers();
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.AllowSynchronousIO = true;
                serverOptions.Listen(new(IPAddress.Parse(builder.Configuration.GetValue("Server:Address", "127.0.0.1") ?? throw new InvalidOperationException("Server:Address is null")), builder.Configuration.GetValue<ushort>("Server:Port", 8080)));
            });

            WebApplication app = builder.Build();
            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    // TODO: PollMaster3000.Docs, using csproj Tasks, docfx and possibly github actions.
            //    FileProvider =
            //})
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers().RequireAuthorization("Discord");

            return app.RunAsync();
        }
    }
}
