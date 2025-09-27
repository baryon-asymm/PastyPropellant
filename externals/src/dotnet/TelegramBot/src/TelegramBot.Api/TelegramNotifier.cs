using PastyPropellant.Core.Utils;
using System.Collections.Concurrent;
using Telegram.Bot;
using TelegramBot.Api.Interfaces;

namespace TelegramBot.Api;

public class TelegramNotifierService : ITelegramNotifierService
{
    private static ITelegramNotifierService? _instance;

    public static ITelegramNotifierService GetInstance()
    {
        if (_instance is null)
            throw new InvalidOperationException("Instance not created");

        return _instance;
    }

    public static ITelegramNotifierService GetInstance(string botToken, string chatId, TimeSpan notifyTimeout)
    {
        if (_instance is not null)
            throw new InvalidOperationException("Instance already created");

        return _instance ??= new TelegramNotifierService(botToken, chatId, notifyTimeout);
    }

    private readonly TelegramBotClient _botClient;
    private readonly string _chatId;
    private readonly TimeSpan _notifyTimeout;

    private DateTime _lastMessageTime = DateTime.MinValue;

    private readonly ConcurrentQueue<string> _messages = new();

    public TelegramNotifierService(string botToken, string chatId, TimeSpan notifyTimeout) =>
        (_botClient, _chatId, _notifyTimeout) = (new TelegramBotClient(botToken), chatId, notifyTimeout);

    public void Notify(string message) => _messages.Enqueue(message);

    public async Task<OperationResult> RunAsync()
    {
        while (true)
        {
            if (_messages.TryDequeue(out var message))
            {
                var operationResult = await NotifyAsync(message);
                if (operationResult.IsSuccess == false)
                    throw operationResult.Exception;
            }
        }
    }

    private async Task<OperationResult> NotifyAsync(string message)
    {
        try
        {
            var currentTime = DateTime.Now;
            var timeSinceLastMessage = currentTime - _lastMessageTime;

            if (timeSinceLastMessage < _notifyTimeout)
            {
                var waitTime = _notifyTimeout - timeSinceLastMessage;
                Thread.Sleep(waitTime);
            }

            _lastMessageTime = DateTime.Now;

            await TrySendMessageAsync(message);

            return new();
        }
        catch (Exception ex)
        {
            return new(ex);
        }
    }

    private async Task TrySendMessageAsync(string message)
    {
        await _botClient.SendTextMessageAsync(_chatId, message);
    }
}
