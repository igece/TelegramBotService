using System.Threading;
using System.Threading.Tasks;


namespace TelegramBotService.Commands
{
    public interface ICommand
    {
        string Name { get; }

        Task Execute(long userId, string request, CancellationToken cancellationToken = default);
    }
}
