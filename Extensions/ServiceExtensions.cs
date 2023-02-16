using System.Reflection;

using TelegramBotService.Commands;
using TelegramBotService.Services;


namespace TelegramBotService.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddBackgroundService<T>(this IServiceCollection services) where T : IHostedService
        {
            services.AddSingleton(typeof(T));
            services.AddHostedService<BackgroundServiceStarter<T>>();

            return services;
        }


        public static IServiceCollection AddCommands(this IServiceCollection serviceCollection)
        {
            foreach (var command in Assembly.GetExecutingAssembly().GetTypes().Where(command => command.BaseType == typeof(BaseCommand)))
                serviceCollection.AddTransient(command);

            return serviceCollection;
        }
    }
}