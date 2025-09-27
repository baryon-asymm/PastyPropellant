using PastyPropellant.Core.Utils;

namespace TelegramBot.Api.Interfaces;

public interface ITelegramNotifierService
{
    public Task<OperationResult> RunAsync();
    public void Notify(string message);
}
