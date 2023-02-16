using TelegramBotService.Commands;


namespace TelegramBotService.Services
{
    public class CommandService
    {
        private readonly IServiceProvider _serviceProvider;


        public CommandService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

        }


        public ICommand? InstanceCommand(string request)
        {
            if (string.IsNullOrEmpty(request))
                return null;

            var args = GetArguments(request, true);

            if (args.Length == 0)
                return null;

            var commandStr = args[0].TrimStart('/').ToLowerInvariant();
            var commandType = Type.GetType("TelegramBotService.Commands." + commandStr, false, true);

            if (commandType != null)
                return (ICommand?)_serviceProvider.GetService(commandType);

            return null;
        }


        public string[] GetArguments(string request, bool includeCommand = false)
        {
            var args = request.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return includeCommand ? args : args.Skip(1).ToArray();
        }
    }
}
