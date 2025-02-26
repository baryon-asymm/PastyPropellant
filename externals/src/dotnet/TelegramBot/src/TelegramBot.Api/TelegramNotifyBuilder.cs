using TelegramBot.Api.DTOs;
using TelegramBot.Api.Interfaces;

namespace TelegramBot.Api;

public interface IBuilder
{
    public ITelegramNotifierService Build();
}

public class TelegramNotifyBuilder : IBuilder
{
    private readonly TelegramBotSettings _settings;

    private TelegramNotifyBuilder(TelegramBotSettings settings) =>
        _settings = settings;

    public static IBuilder FromSettings(TelegramBotSettings settings) =>
        new TelegramNotifyBuilder(settings);

    public ITelegramNotifierService Build()
    {
        return new TelegramNotifierService(_settings.BotToken,
                                           _settings.ChatId,
                                           _settings.NotifyTimeout);
    }
}
