using PastyPropellant.Core.Utils;

namespace TelegramBot.Service.Adapters.Interfaces;

public interface ITelegramAdapter
{
    public Task<OperationResult> SendAsync(string message);
}
