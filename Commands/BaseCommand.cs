using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Telegram.Bot;


namespace TelegramBotService.Commands
{
  abstract class BaseCommand : ICommand
  {
    public abstract string Name { get; }

    public string Request { get; private set; }


    public async Task Execute(string request, ITelegramBotClient client, long chatId, ILogger logger, IServiceProvider serviceProvider)
    {
      Request = request;
      var args = GetArguments(request);

      try
      {
        await Execute(args, client, chatId, logger, serviceProvider);
      }

      catch (Exception ex)
      {
        logger.LogError($"Cannot execute command /{Name}", ex);
        await client.SendTextMessageAsync(chatId, string.Format(Resources.Strings.ErrorProcessingRequest, ex.Message));
      }       
    }


    protected abstract Task Execute(string[] args, ITelegramBotClient client, long chatId, ILogger logger, IServiceProvider serviceProvider);


    private string[] GetArguments(string request)
    {
      var args = request.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      return args.Length > 1 ? args.Skip(1).ToArray() : Array.Empty<string>();
    }
  }
}
