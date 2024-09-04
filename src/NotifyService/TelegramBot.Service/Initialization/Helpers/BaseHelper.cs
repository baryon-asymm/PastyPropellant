using Newtonsoft.Json;
using PastyPropellant.Core.Utils;

namespace TelegramBot.Service.Initialization.Helpers;

public abstract class BaseHelper<T> where T : class
{
    private readonly string _filePath;

    public BaseHelper(string filePath)
    {
        _filePath = filePath;
    }

    public OperationResult<T> Read()
    {
        try
        {
            var item = TryRead();

            if (item is null)
                throw new ArgumentNullException($"Failed to read {nameof(T)}.");

            return new OperationResult<T>(item);
        }
        catch (Exception ex)
        {
            return new OperationResult<T>(ex);
        }
    }

    private T? TryRead()
    {
        var json = File.ReadAllText(_filePath);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
