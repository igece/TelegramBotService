using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Telegram.Bot;


namespace TelegramBotService.Commands
{
  public interface ICommand
  {
    string Name { get; }

    Task Execute(string request, ITelegramBotClient client, long chatId, ILogger logger, IServiceProvider serviceProvider);
  }
}
