using TelegramBot.Service.Models;

namespace TelegramBot.Service.Initialization.Helpers;

public class TelegramSettingsHelper : BaseHelper<TelegramSettings>
{
    public TelegramSettingsHelper(string filePath) : base(filePath)
    {
    }
}
