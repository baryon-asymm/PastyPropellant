using System.Collections.Concurrent;
using PastyPropellant.Core.Utils;
using TelegramBot.Service.Adapters.Interfaces;
using TelegramBot.Service.Models;
using TelegramBot.Service.Shared.Models;

namespace TelegramBot.Service.Services;

public class NotifyService : IHostedService
{
    private readonly Dictionary<Guid, Client> _clientsMap = new();
    private readonly ILogger<NotifyService> _logger;
    private readonly ConcurrentQueue<string> _messages = new();

    private readonly TelegramSettings _settings;
    private readonly ITelegramAdapter _telegramAdapter;

    private bool _isRunning = true;

    public NotifyService(
        ILogger<NotifyService> logger,
        TelegramSettings settings,
        ITelegramAdapter telegramAdapter,
        List<Client> clients)
    {
        _logger = logger;
        _settings = settings;
        _telegramAdapter = telegramAdapter;

        foreach (var client in clients)
            _clientsMap.Add(client.Token, client);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        EventBus<NotifyRequest>.Subscribe(OnNotifyRequest);
        Task.Factory.StartNew(RunAsync, TaskCreationOptions.LongRunning);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isRunning = false;
        EventBus<NotifyRequest>.Unsubscribe(OnNotifyRequest);
        return Task.CompletedTask;
    }

    private void OnNotifyRequest(NotifyRequest request)
    {
        _logger.LogInformation("Received notification request: {0}", request);

        if (_clientsMap.TryGetValue(request.Token, out var client) == false)
        {
            _logger.LogWarning("Client not found: {0}", request.Token);
            return;
        }

        _messages.Enqueue($"Notify from {client.Name}\n\n{request.Message}");
    }

    private async Task RunAsync()
    {
        var lastMessageTime = DateTime.MinValue;

        while (_isRunning)
        {
            var currentTime = DateTime.Now;
            var timeSinceLastMessage = currentTime - lastMessageTime;

            if (timeSinceLastMessage < _settings.NotifyTimeout)
            {
                var waitTime = _settings.NotifyTimeout - timeSinceLastMessage;
                await Task.Delay(waitTime);
            }

            lastMessageTime = DateTime.Now;

            var notifyMessage = string.Empty;
            while (_messages.TryDequeue(out var message))
            {
                if (string.IsNullOrEmpty(notifyMessage) == false)
                    notifyMessage += "\n\n<!-- separation line -->\n\n";
                notifyMessage += message;
            }

            if (string.IsNullOrEmpty(notifyMessage) == false)
            {
                var operationResult = await _telegramAdapter.SendAsync(notifyMessage);
                if (operationResult.IsSuccess == false)
                    throw operationResult.Exception;
            }
        }

        _logger.LogInformation("Notify service stopped");
    }
}
