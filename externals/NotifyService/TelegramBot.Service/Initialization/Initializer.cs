using PastyPropellant.Core.Utils;
using TelegramBot.Service.Adapters;
using TelegramBot.Service.Adapters.Interfaces;
using TelegramBot.Service.Initialization.Helpers;
using TelegramBot.Service.Services;

namespace TelegramBot.Service.Initialization;

public class Initializer
{
    private const string _clientsFilePath = "clients.json";
    private const string _telegramSettingsFilePath = "telegram_settings.json";

    public static OperationResult Initialize(IServiceCollection services)
    {
        try
        {
            TryInitialize(services);

            return new OperationResult();
        }
        catch (Exception ex)
        {
            return new OperationResult(ex);
        }
    }

    private static void TryInitialize(IServiceCollection services)
    {
        InitClients(services);
        InitTelegramSettings(services);
        InitTelegramAdapter(services);
        InitTelegramNotifierService(services);
    }

    private static void InitClients(IServiceCollection services)
    {
        var clientsHelper = new ClientsHelper(_clientsFilePath);
        AddSingletonModels(services, clientsHelper);
    }

    private static void InitTelegramSettings(IServiceCollection services)
    {
        var telegramSettingsHelper = new TelegramSettingsHelper(_telegramSettingsFilePath);
        AddSingletonModels(services, telegramSettingsHelper);
    }

    private static void AddSingletonModels<T>(IServiceCollection services, BaseHelper<T> helper) where T : class
    {
        var operationResult = helper.Read();

        if (operationResult.IsSuccess == false)
            throw operationResult.Exception;

        services.AddSingleton(operationResult.Value ?? throw new ArgumentNullException(nameof(T)));
    }

    private static void InitTelegramAdapter(IServiceCollection services)
    {
        services.AddSingleton<ITelegramAdapter, TelegramAdapter>();
    }

    private static void InitTelegramNotifierService(IServiceCollection services)
    {
        services.AddHostedService<NotifyService>();
    }
}
