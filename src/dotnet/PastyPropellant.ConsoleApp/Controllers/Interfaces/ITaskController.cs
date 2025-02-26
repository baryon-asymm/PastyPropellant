using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Controllers.Interfaces;

public interface ITaskController
{
    public Task<OperationResult> RunAsync();
}
