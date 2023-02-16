using TelegramBotService.Services;


namespace TelegramBotService.Commands
{
    abstract class BaseCommand : ICommand
    {
        public abstract string Name { get; }

        public string? Request { get; private set; }


        protected ILogger Logger { get; }

        protected BotService BotService { get; }

        protected LocalizationService LocalizedString { get; }


        protected BaseCommand(ILogger logger, LocalizationService localizerService, BotService botService)
        {
            Logger = logger;
            LocalizedString = localizerService;
            BotService = botService;
        }


        public async Task Execute(long userId, string request, CancellationToken cancellationToken = default)
        {
            Request = request;
            var args = GetArguments(request);

            try
            {
                await Execute(userId, args, cancellationToken);
            }

            catch (Exception ex)
            {
                Logger.LogError(ex, "Executing command /{Command}", Name);
                await BotService.SendMessage(userId, string.Format(LocalizedString.ErrorProcessingRequest, ex.Message), cancellationToken);
            }
        }


        protected abstract Task Execute(long userId, string[] args, CancellationToken cancellationToken = default);


        public static string[] GetArguments(string request, bool includeCommand = false)
        {
            var args = request.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return includeCommand ? args : args.Skip(1).ToArray();
        }
    }
}
