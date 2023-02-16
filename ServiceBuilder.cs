using System.Text.Json.Serialization;

using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

using TelegramBotService.Extensions;
using TelegramBotService.Services;


namespace TelegramBotService
{
    internal class ServiceBuilder
    {
        private WebApplication? _app;


        public void Start(string[] args)
        {
            // In Windows, network services set working directory to c:\windows\system32.
            if (OperatingSystem.IsWindows())
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog as logging engine.
            // Read configuration from appsettings.json, but always log to console.

            builder.Host.UseSerilog((builderContext, loggerConf) => loggerConf
                .ReadFrom.Configuration(builder.Configuration, sectionName: "Logger")
                .WriteTo.Console(theme: SystemConsoleTheme.Colored)
                .Enrich.WithProperty("Application", "CamMonitor.Bot"));

            builder.Services.AddLocalization(config => config.ResourcesPath = "Resources");
            builder.Services.AddTransient<LocalizationService>();

            builder.Services.AddSingleton<ConfigurationService>();
            builder.Services.AddTransient<CommandService>();
            builder.Services.AddCommands();

            builder.Services.AddBackgroundService<BotService>();

            builder.Services.AddControllers().AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            _app = builder.Build();

            _app.RunAsync();
        }


        public void Stop()
        {
            _app?.StopAsync().Wait();
            _app?.DisposeAsync().AsTask().Wait();
        }
    }
}
