using TelegramBotService;

using Topshelf;


var serviceName = "Sample Telegram Bot";
var serviceDescription = "A sample Telegram bot.";



HostFactory.Run(hostService =>
{
    hostService.Service<ServiceBuilder>(svcConfig =>
    {
        svcConfig.ConstructUsing(_ => new ServiceBuilder());
        svcConfig.WhenStarted(service => service.Start(args));
        svcConfig.WhenStopped(service => service.Stop());
    });

    hostService.EnableServiceRecovery(recovery =>
    {
        recovery.RestartService(0);
        recovery.RestartService(1);
        recovery.RestartService(1);
    });

    hostService.SetDescription(serviceDescription);
    hostService.SetDisplayName(serviceName);

#if DEBUG
    hostService.SetServiceName($"{serviceName}_{Guid.NewGuid():N}");
#else
    hostService.SetServiceName(serviceName);
# endif

    hostService.EnableShutdown();
    hostService.StartAutomaticallyDelayed();
    hostService.RunAsNetworkService();
});
