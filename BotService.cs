using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

using TelegramBotService.Commands;


namespace TelegramBotService
{
  class BotService
  {
    protected IServiceProvider ServiceProvider { get; private set; }

    protected ILogger Logger { get; private set; }

    protected ITelegramBotClient BotClient { get; private set; }

    private long? chatId_ = null;


    public BotService(IServiceProvider serviceProvider)
    {
      ServiceProvider = serviceProvider;
      Logger = serviceProvider.GetRequiredService<ILogger<BotService>>();
      BotClient = serviceProvider.GetRequiredService<ITelegramBotClient>();
    }


    public void Start()
    {
      Logger.LogInformation("Starting bot service");

      string appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

      using (var scope = ServiceProvider.CreateScope())
      {
        using var db = scope.ServiceProvider.GetRequiredService<BotContext>();
        var storedChatId = db.ReadSetting<long>("ChatId", -1);

        if (storedChatId != -1)
          chatId_ = storedChatId;
        else
          chatId_ = null;
      }



      if (chatId_.HasValue)
        BotClient.SendTextMessageAsync(chatId_, Resources.Strings.BotStarted);

      BotClient.OnMessage += OnMessageReceived;
      BotClient.MakingApiRequest += BotClient_MakingApiRequest;

      if (Utils.CanConnectToTelegram())
        BotClient.StartReceiving();

      NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
    
      Logger.LogInformation("Bot service started");
    }


    private void BotClient_MakingApiRequest(object sender, ApiRequestEventArgs e)
    {
      Task.Delay(250).Wait();
    }


    public void Stop()
    {
      Logger.LogInformation("Bot service stopped");
      
      NetworkChange.NetworkAddressChanged -= NetworkAddressChanged;

      BotClient.StopReceiving();
      BotClient.OnMessage -= OnMessageReceived;
      BotClient.MakingApiRequest -= BotClient_MakingApiRequest;
    }


    private void NetworkAddressChanged(object sender, EventArgs e)
    {
      if (!BotClient.IsReceiving && Utils.CanConnectToTelegram())
        BotClient.StartReceiving();

      else if (BotClient.IsReceiving && !Utils.CanConnectToTelegram())
        BotClient.StopReceiving();
    }


    private async void OnMessageReceived(object sender, MessageEventArgs e)
    {
      if (string.IsNullOrEmpty(e.Message.Text))
        return;

      if (e.Message.Chat.Type != ChatType.Private)
      {
        Logger.LogWarning($"Ignoring {e.Message.Chat.Type} message from {e.Message.Contact.PhoneNumber} ({e.Message.Contact.FirstName} {e.Message.Contact.LastName}, UserId: {e.Message.Contact.UserId})");
        return;
      }

      if ((chatId_ == null) || (chatId_ != e.Message.Chat.Id))
      {
        chatId_ = e.Message.Chat.Id;

        using (var scope = ServiceProvider.CreateScope())
        {
          using var db = scope.ServiceProvider.GetRequiredService<BotContext>();
          db.StoreSetting("ChatId", chatId_);
          db.SaveChanges();
        }

        await BotClient.SendTextMessageAsync(chatId_, Resources.Strings.WelcomeMessage);
      }

      var messageText = e.Message.ForwardFrom == null ? e.Message.Text : e.Message.Caption;

      try
      {
        var cmd = CommandFactory.FromText(messageText);

        if (cmd != null)
        {
          Logger.LogInformation($"Received command '{cmd.Name}'\n{messageText}");
          await cmd.Execute(messageText, BotClient, e.Message.Chat.Id, Logger, ServiceProvider);
        }
      }

      catch (Exception ex)
      {
        Logger.LogError(ex, $"Unable to process incoming message '{messageText}'");
      }
    }








  }
}
