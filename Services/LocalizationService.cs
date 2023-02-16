using Microsoft.Extensions.Localization;


// Will not work if not defined in project's root namespace.
namespace TelegramBotService
{
    public class Localization
    {
        // Empty class used to group all localization strings in a single (Localization.resx) resource file.
    }
}


namespace TelegramBotService.Services
{
    public class LocalizationService
    {
        public string BotStarted => _localizer["BotStarted"];

        public string ErrorProcessingRequest => _localizer["ErrorProcessingRequest"];

        public string Pong => _localizer["Pong"];

        public string UnknownCommand => _localizer["UnknownCommand"];

        public string WelcomeMessage => _localizer["WelcomeMessage"];


        public string Localize(string name) => _localizer[name];


        private readonly IStringLocalizer<Localization> _localizer;


        public LocalizationService(IStringLocalizer<Localization> localizer)
        {
            _localizer = localizer;
        }
    }
}
