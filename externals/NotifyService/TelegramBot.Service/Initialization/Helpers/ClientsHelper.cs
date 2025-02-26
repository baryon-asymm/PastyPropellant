using TelegramBot.Service.Models;

namespace TelegramBot.Service.Initialization.Helpers;

public class ClientsHelper : BaseHelper<List<Client>>
{
    public ClientsHelper(string filePath) : base(filePath)
    {
    }
}
