namespace TelegramBotService.Services
{
    public class BackgroundServiceStarter<T> : IHostedService where T : IHostedService
    {
        private readonly T _backgroundService;


        public BackgroundServiceStarter(T backgroundService)
        {
            _backgroundService = backgroundService;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _backgroundService.StartAsync(cancellationToken);
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _backgroundService.StopAsync(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
