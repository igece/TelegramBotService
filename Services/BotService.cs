using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

using TelegramBotService.Model;

using MediaType = TelegramBotService.Model.Enums.MediaType;
using Message = TelegramBotService.Model.Message;


namespace TelegramBotService.Services
{
    public class BotService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly TelegramBotClient _botClient;

        private readonly IEnumerable<long> _allowedUsers;

        private readonly HttpClient _httpClient;

        private readonly LocalizationService _localization;

        private bool _disposed;


        public BotService(ILogger<BotService> logger, IServiceProvider serviceProvider, ConfigurationService configuration, LocalizationService localization)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(3000)
            };

            _botClient = new TelegramBotClient(configuration.BotToken);
            _allowedUsers = configuration.AllowedUsers;
            _localization = localization;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (await CanConnectToTelegram())
                _botClient.StartReceiving(OnIncomingMessage, OnException, cancellationToken: cancellationToken);
            else
                _logger.LogWarning("Unable to connect to Telegram API endpoint");

            //NetworkChange.NetworkAddressChanged += NetworkAddressChanged;

            foreach (var user in _allowedUsers)
                await SendMessage(user, _localization.BotStarted, cancellationToken);
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_botClient.StopReceiving();
            return Task.CompletedTask;
        }


        public async Task SendMessage(Message message, CancellationToken cancellationToken = default)
        {
            await SendMessage(message.Text, cancellationToken);
        }


        public Task SendMessage(string message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(message);

            return SendMessageInternal(message, cancellationToken);


            async Task SendMessageInternal(string message, CancellationToken cancellationToken = default)
            {
                foreach (var user in _allowedUsers)
                    await _botClient.SendTextMessageAsync(user, message, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
        }


        public async Task SendMessage(long userId, Message message, CancellationToken cancellationToken = default)
        {
            await SendMessage(userId, message.Text, cancellationToken);
        }


        public Task SendMessage(long userId, string message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(message);

            return SendMessageInternal(userId, message, cancellationToken);


            async Task SendMessageInternal(long userId, string message, CancellationToken cancellationToken = default)
            {
                await _botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }
        }


        public Task SendMedia(Media media, CancellationToken cancellationToken = default)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            return SendMediaInternal(media, cancellationToken);


            async Task SendMediaInternal(Media media, CancellationToken cancellationToken = default)
            {
                using var stream = new MemoryStream(media.Data);
                var mediaName = media.FileName;

                switch (media.Type)
                {
                    case MediaType.Image:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendPhotoAsync(user, new InputOnlineFile(stream, mediaName), media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;

                    case MediaType.Audio:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendVoiceAsync(user, new InputOnlineFile(stream, mediaName), media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;

                    case MediaType.Video:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendVideoAsync(user, new InputOnlineFile(stream, mediaName), caption: media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;

                    default:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendDocumentAsync(user, new InputOnlineFile(stream, mediaName), caption: media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;
                }

            }
        }


        public Task SendMedia(IEnumerable<Media> mediaCollection, CancellationToken cancellationToken = default)
        {
            if (mediaCollection == null)
                throw new ArgumentNullException(nameof(mediaCollection));

            return SendMediaInternal(mediaCollection, cancellationToken);


            async Task SendMediaInternal(IEnumerable<Media> mediaCollection, CancellationToken cancellationToken = default)
            {
                var items = mediaCollection.Count();

                if (items == 0)
                    return;

                if (items == 1)
                {
                    await SendMedia(mediaCollection.First(), cancellationToken);
                    return;
                }

                if (items > 10)
                    _logger.LogWarning("Sending a media collection of more than 10 elements, only the first 10 will be sent");

                var inputMedia = new List<IAlbumInputMedia>();
                var memStreams = new List<MemoryStream>();
                var firstMedia = true;

                foreach (var media in mediaCollection.Take(10))
                {
                    var memStream = new MemoryStream(media.Data);
                    memStreams.Add(memStream);

                    switch (media.Type)
                    {
                        case MediaType.Image:

                            inputMedia.Add(new InputMediaPhoto(new InputMedia(memStream, media.FileName ?? Guid.NewGuid().ToString("N")))
                            { Caption = firstMedia ? media.Text : null, ParseMode = ParseMode.Markdown });
                            firstMedia = false;

                            break;

                        case MediaType.Video:

                            inputMedia.Add(new InputMediaVideo(new InputMedia(memStream, media.FileName ?? Guid.NewGuid().ToString("N")))
                            { Caption = firstMedia ? media.Text : null, ParseMode = ParseMode.Markdown });
                            firstMedia = false;

                            break;

                        default:

                            _logger.LogError("Only image or video media can be added to a Telegram media collection");
                            break;
                    }
                }

                try
                {
                    foreach (var user in _allowedUsers)
                        await _botClient.SendMediaGroupAsync(user, inputMedia, cancellationToken: cancellationToken);
                }

                finally
                {
                    memStreams.ForEach(ms => ms.Dispose());
                }
            }
        }


        public Task SendMedia(string file, MediaType mediaType, string? caption = null, string? name = null, CancellationToken cancellationToken = default)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return SendMediaInternal(file, mediaType, caption, name, cancellationToken);


            async Task SendMediaInternal(string file, MediaType mediaType, string? caption = null, string? name = null, CancellationToken cancellationToken = default)
            {
                using var stream = new FileInfo(file).OpenRead();
                var mediaName = name ?? Path.GetFileName(file);

                switch (mediaType)
                {
                    case MediaType.Image:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendPhotoAsync(user, new InputOnlineFile(stream, mediaName), caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;

                    case MediaType.Audio:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendVoiceAsync(user, new InputOnlineFile(stream, mediaName), caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;

                    case MediaType.Video:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendVideoAsync(user, new InputOnlineFile(stream, mediaName), caption: caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;

                    default:

                        foreach (var user in _allowedUsers)
                            await _botClient.SendDocumentAsync(user, new InputOnlineFile(stream, mediaName), caption: caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                        break;
                }
            }
        }


        public Task SendMedia(long userId, Media media, CancellationToken cancellationToken = default)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            return SendMediaInternal(userId, media, cancellationToken);


            async Task SendMediaInternal(long userId, Media media, CancellationToken cancellationToken = default)
            {
                using var stream = new MemoryStream(media.Data);
                var mediaName = media.FileName;

                _ = media.Type switch
                {
                    MediaType.Image =>
                        await _botClient.SendPhotoAsync(userId, new InputOnlineFile(stream, mediaName), media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),

                    MediaType.Audio =>
                        await _botClient.SendVoiceAsync(userId, new InputOnlineFile(stream, mediaName), media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),

                    MediaType.Video =>
                        await _botClient.SendVideoAsync(userId, new InputOnlineFile(stream, mediaName), caption: media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),

                    _ => await _botClient.SendDocumentAsync(userId, new InputOnlineFile(stream, mediaName), media.Text, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),
                };
            }
        }


        public Task SendMedia(long userId, IEnumerable<Media> mediaCollection, CancellationToken cancellationToken = default)
        {
            if (mediaCollection == null)
                throw new ArgumentNullException(nameof(mediaCollection));

            return SendMediaInternal(userId, mediaCollection, cancellationToken);


            async Task SendMediaInternal(long userId, IEnumerable<Media> mediaCollection, CancellationToken cancellationToken = default)
            {
                var items = mediaCollection.Count();

                if (items == 0)
                    return;

                if (items == 1)
                {
                    await SendMedia(userId, mediaCollection.First(), cancellationToken);
                    return;
                }

                if (items > 10)
                    _logger.LogWarning("Sending a media collection of more than 10 elements, only the first 10 will be sent");

                var inputMedia = new List<IAlbumInputMedia>();
                var memStreams = new List<MemoryStream>();
                var firstMedia = true;

                foreach (var media in mediaCollection.Take(10))
                {
                    var memStream = new MemoryStream(media.Data);
                    memStreams.Add(memStream);

                    switch (media.Type)
                    {
                        case MediaType.Image:

                            inputMedia.Add(new InputMediaPhoto(new InputMedia(memStream, media.FileName ?? Guid.NewGuid().ToString("N")))
                            { Caption = firstMedia ? media.Text : null, ParseMode = ParseMode.Markdown });
                            firstMedia = false;

                            break;

                        case MediaType.Video:

                            inputMedia.Add(new InputMediaVideo(new InputMedia(memStream, media.FileName ?? Guid.NewGuid().ToString("N")))
                            { Caption = firstMedia ? media.Text : null, ParseMode = ParseMode.Markdown });
                            firstMedia = false;

                            break;

                        default:

                            _logger.LogError("Only image or video media can be added to a Telegram media collection");
                            break;
                    }
                }

                try
                {
                    await _botClient.SendMediaGroupAsync(userId, inputMedia, cancellationToken: cancellationToken);
                }

                finally
                {
                    memStreams.ForEach(ms => ms.Dispose());
                }
            }
        }


        public Task SendMedia(long userId, string file, MediaType mediaType, string? caption = null, string? name = null, CancellationToken cancellationToken = default)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return SendMediaInternal(userId, file, mediaType, caption, name, cancellationToken);


            async Task SendMediaInternal(long userId, string file, MediaType mediaType, string? caption = null, string? name = null, CancellationToken cancellationToken = default)
            {
                using var stream = new FileInfo(file).OpenRead();
                var mediaName = name ?? Path.GetFileName(file);

                _ = mediaType switch
                {
                    MediaType.Image =>
                        await _botClient.SendPhotoAsync(userId, new InputOnlineFile(stream, mediaName), caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),

                    MediaType.Audio =>
                        await _botClient.SendVoiceAsync(userId, new InputOnlineFile(stream, mediaName), caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),

                    MediaType.Video =>
                        await _botClient.SendVideoAsync(userId, new InputOnlineFile(stream, mediaName), caption: caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),

                    _ => await _botClient.SendDocumentAsync(userId, new InputOnlineFile(stream, mediaName), caption, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken),
                };
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        private async Task<bool> CanConnectToTelegram()
        {
            try
            {
                await _httpClient.GetAsync("https://api.telegram.org");
            }

            catch (HttpRequestException)
            {
                return false;
            }

            catch (TaskCanceledException)
            {
                return false;
            }

            return true;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _httpClient?.Dispose();

                _disposed = true;
            }
        }


        /*
        private async void NetworkAddressChanged(object? sender, EventArgs e)
        {
            if (!_botClient.IsReceiving && await CanConnectToTelegram())
            {
                _logger.LogWarning($"Network address change detected: Telegram API endpoint available, starting bot client");
                _botClient.StartReceiving();
            }

            else if (_botClient.IsReceiving && !await CanConnectToTelegram())
            {
                _logger.LogWarning($"Network address change detected: Telegram API endpoint not available, stopping bot client");
                _botClient.StopReceiving();
            }
        }
        */


        private async Task OnIncomingMessage(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            if (!_allowedUsers.Contains(update.Message.From.Id))
            {
                _logger.LogWarning("Ignoring message from unauthorized user {Username} ({FullName}, UserId: {UserId})",
                    update.Message.From.Username, string.Concat(update.Message.From.FirstName, " ", update.Message.From.LastName), update.Message.From.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(update.Message.Text) && update.Message.ForwardFrom == null)
                return;

            if (update.Message.Chat.Type != ChatType.Private)
            {
                _logger.LogWarning("Ignoring message from non-private chat");
                return;
            }

            var messageText = update.Message.Type == MessageType.Photo || update.Message.Type == MessageType.Video ?
                update.Message.Caption ?? string.Empty :
                update.Message.Text ?? string.Empty;

            if (update.Message.ReplyToMessage?.Caption is string replyMessage)
            {
                var cameraIdMatch = Regex.Match(replyMessage, @"[A-Z]{5}");

                if (cameraIdMatch.Success)
                    messageText += $" {cameraIdMatch.Value}";
            }

            if (update.Message.Entities != null)
            {
                foreach (var entity in update.Message.Entities.Where(e => e.Type == MessageEntityType.TextLink))
                    messageText = messageText.Replace(messageText.Substring(entity.Offset, entity.Length), entity.Url);
            }

            if (update.Message.CaptionEntities != null)
            {
                foreach (var entity in update.Message.CaptionEntities.Where(e => e.Type == MessageEntityType.TextLink))
                    messageText = messageText.Replace(messageText.Substring(entity.Offset, entity.Length), entity.Url);
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var commandService = scope.ServiceProvider.GetRequiredService<CommandService>();

                var cmd = commandService.InstanceCommand(messageText);

                if (cmd != null)
                {
                    _logger.LogInformation("Received command '{Command}'\n{OriginalText}", cmd.Name, messageText);
                    await cmd.Execute(update.Message.From.Id, messageText, cancellationToken);
                }

                else
                {
                    var args = commandService.GetArguments(messageText, true);

                    if (args[0].StartsWith("/"))
                    {
                        _logger.LogWarning("Received unknown command '{Command}'\n{OriginalText}", args[0], messageText);
                        await SendMessage(update.Message.From.Id, _localization.UnknownCommand, cancellationToken);
                    }
                    else
                        _logger.LogWarning("Received unknown request\n{Message}", messageText);
                }
            }
        }


        private Task OnException(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
