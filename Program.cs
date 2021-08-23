using System;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using Telegram.Bot;

using Topshelf;


namespace TelegramBotService
{
  class Program
  {
    static void Main(string[] args)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
      var builder = new ConfigurationBuilder()
          .AddJsonFile($"Settings.json", true, true)
          .AddJsonFile($"Settings.{env}.json", true, true)
          .AddEnvironmentVariables();

      var configuration = builder.Build();

      var services = new ServiceCollection();
      ConfigureServices(services, configuration);

      using var serviceProvider = services.BuildServiceProvider();
      
      using (var scope = serviceProvider.CreateScope())
      {
        var svc = scope.ServiceProvider;

        try
        {
          svc.GetRequiredService<BotContext>().Database.EnsureCreated();
        }

        catch (Exception ex)
        {
          svc.GetRequiredService<ILogger<Program>>().LogCritical(ex, "Error in database initialization");
        }
      }
      
      HostFactory.Run(botService =>
      {
        botService.Service<BotService>(svcConfig =>
        {
          svcConfig.ConstructUsing(_ => new BotService(serviceProvider));
          svcConfig.WhenStarted(service => service.Start());
          svcConfig.WhenStopped(service => service.Stop());
        });

        botService.EnableServiceRecovery(recovery =>
        {
          recovery.RestartService(0);
          recovery.RestartService(0);
          recovery.RestartService(0);
        });
        
        botService.SetServiceName(configuration.GetValue<string>("Service:Name"));
        botService.SetDisplayName(configuration.GetValue<string>("Service:DisplayName"));
        botService.SetDescription(configuration.GetValue<string>("Service:Description"));

        botService.EnableShutdown();

        botService.StartAutomatically();

        botService.RunAsPrompt();
      });
    }


    private static void ConfigureServices(IServiceCollection services, IConfigurationRoot configuration)
    {
      // Setup logging.

      var logFile = configuration.GetValue<string>("Logging:File");
      logFile = Environment.ExpandEnvironmentVariables(logFile);

      var loggingBuilder = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 512 * 1024, rollOnFileSizeLimit: true, retainedFileCountLimit: 10)
        .CreateLogger();

      services.AddLogging(logging => logging.AddSerilog(loggingBuilder));

      var dbPath = configuration.GetValue<string>("Database:Path");
      var botToken = configuration.GetValue<string>("Bot:Token");

      if (string.IsNullOrEmpty(botToken))
        throw new Exception("Telegram bot token not specified");

      services.AddDbContext<BotContext>(options => options.UseSqlite($"Data Source={dbPath}"));
      services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
    }
  }
}
