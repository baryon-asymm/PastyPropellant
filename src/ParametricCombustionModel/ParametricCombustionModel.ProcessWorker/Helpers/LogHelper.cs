using System.Net.Http.Json;
using PastyPropellant.Core.Models.Events;
using TelegramBot.Service.Shared.Models;

namespace ParametricCombustionModel.ProcessWorker.Helpers;

public static class LogHelper
{
    private const string _notifyUrl = "http://192.168.255.250:8888/api/notify";
    private static readonly Guid _token = Guid.Parse("ee1f2e57-db0e-4bdd-a63e-0dd559599be3");
    private static readonly HttpClient _notifyHelper = new();

    public static void ToLog(ref LogEvent logEvent)
    {
        var message = logEvent.Message.ToString();
        _notifyHelper.PostAsJsonAsync(_notifyUrl, new NotifyRequest(_token, message)).Wait();
        Console.WriteLine(message);
    }
}
