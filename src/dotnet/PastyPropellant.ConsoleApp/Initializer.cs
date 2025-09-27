using System.Text.Json.Serialization;
using PastyPropellant.ConsoleApp.Builders.Controllers.Optimization;
using PastyPropellant.ConsoleApp.Controllers.Optimization;
using PastyPropellant.Core.Models.Events;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp;

public class Initializer
{
    private readonly HttpClient _httpClient = new();
    private readonly string _optimizationFilePath;
    private readonly string _telegramSettingsFilePath;

    private OptimizationController _optimizationController;

    public Initializer(string optimizationFilePath, string telegramSettingsFilePath)
    {
        (_optimizationFilePath, _telegramSettingsFilePath) = (optimizationFilePath, telegramSettingsFilePath);
    }

    public async Task<OperationResult> InitializeAsync()
    {
        try
        {
            await TryInitializeAsync();
            return new OperationResult();
        }
        catch (Exception ex)
        {
            return new OperationResult(ex);
        }
    }

    private async Task TryInitializeAsync()
    {
        await CreateOptimizationControllerAsync();
        await CreateTelegramNotifierServiceAsync();

        EventBus<LogEvent>.Subscribe(msg => Console.WriteLine(msg.Message));
    }

    private async Task CreateOptimizationControllerAsync()
    {
        var builder = OptimizationControllerBuilder.FromFilePath(_optimizationFilePath);
        _optimizationController = await builder.WithProcessorCount(4)
                                               .BuildAsync();
    }

    private async Task CreateTelegramNotifierServiceAsync()
    {
        await Task.Yield();

        _httpClient.BaseAddress = new Uri("http://192.168.255.250:8888/");

#if RELEASE
        EventBus<LogEvent>.Subscribe(async (msg) =>
        {
            var token = Guid.Parse("ee1f2e57-db0e-4bdd-a63e-0dd559599be3");
            var result =
 await _httpClient.PostAsJsonAsync("/api/notify", new NotifyRequest(token, msg.Message.Span.ToString()));
        });
#endif
    }

    public async Task<OperationResult> RunAsync()
    {
        try
        {
            return await TryRunAsync();
        }
        catch (Exception ex)
        {
            return new OperationResult(ex);
        }
    }

    private Task<OperationResult> TryRunAsync()
    {
        return _optimizationController.RunAsync();
    }
}

public record NotifyRequest(
    [property: JsonPropertyName("token")]
    [property: JsonRequired]
    Guid Token,
    [property: JsonPropertyName("message")]
    [property: JsonRequired]
    string Message
);
