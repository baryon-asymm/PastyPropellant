using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Interfaces;

public interface IReader<T>
where T : class
{
    public Task<OperationResult<T>> ReadAllAsync();
}
