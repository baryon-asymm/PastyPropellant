using System.Text.Json;
using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Utils.FileReaders;

public class BaseFileModelReader<T>(
    string filePath
) : IModelReader<T>
    where T : class
{
    public string FilePath { get; init; } = filePath;

    public Task<OperationResult<T>> ReadAllAsync()
    {
        try
        {
            return TryReadAllAsync();
        }
        catch (Exception exception)
        {
            return new Task<OperationResult<T>>(default, exception);
        }
    }

    private async Task<OperationResult<T>> TryReadAllAsync()
    {
        await using FileStream fileStream = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = await JsonSerializer.DeserializeAsync<T>(fileStream);
        return new OperationResult<T>(result);
    }
}
