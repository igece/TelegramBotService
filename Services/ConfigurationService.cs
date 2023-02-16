namespace TelegramBotService.Services
{
    public class ConfigurationService
    {
        public string BotToken { get; }

        public IEnumerable<long> AllowedUsers { get; }


        public ConfigurationService(IConfiguration configuration)
        {
            BotToken = configuration.GetValue<string>("Bot:Token");
            AllowedUsers = configuration.GetSection("Bot:AllowedUsers").Get<long[]>().AsEnumerable();
        }
    }
}