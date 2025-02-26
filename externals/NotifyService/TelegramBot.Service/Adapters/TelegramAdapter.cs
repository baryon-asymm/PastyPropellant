using PastyPropellant.Core.Utils;
using Telegram.Bot;
using TelegramBot.Service.Adapters.Interfaces;
using TelegramBot.Service.Models;

namespace TelegramBot.Service.Adapters;

public class TelegramAdapter : ITelegramAdapter
{
    private readonly ILogger<TelegramAdapter> _logger;
    private readonly TelegramSettings _settings;

    private TelegramBotClient? _telegramClient;

    public TelegramAdapter(ILogger<TelegramAdapter> logger, TelegramSettings settings)
    {
        (_logger, _settings) = (logger, settings);
    }

    public async Task<OperationResult> SendAsync(string message)
    {
        try
        {
            await TrySendAsync(message);

            return new OperationResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message");

            _telegramClient = null;
            GC.Collect();

            return new OperationResult(ex);
        }
    }

    private async Task TrySendAsync(string message)
    {
        if (_telegramClient is null) _telegramClient = new TelegramBotClient(_settings.BotToken);

        await _telegramClient.SendTextMessageAsync(_settings.ChatId, message);
    }
}
