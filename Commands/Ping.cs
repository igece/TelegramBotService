using TelegramBotService.Services;


namespace TelegramBotService.Commands
{
    class Ping : BaseCommand
    {
        public override string Name => "ping";


        public Ping(ILogger<Ping> logger, LocalizationService localizationService, BotService botService)
            : base(logger, localizationService, botService)
        {
        }


        protected override Task Execute(long userId, string[] args, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                BotService.SendMessage(LocalizedString.Pong);
            }, cancellationToken);
        }
    }
}