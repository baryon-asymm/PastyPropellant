using PastyPropellant.ConsoleApp.Controllers.ThermodynamicTickets;

namespace PastyPropellant.ConsoleApp.Controllers.Thermodynamic;

public class ThermodynamicTicketsController
{
    private readonly List<ThermodynamicTask> _tasks;

    public ThermodynamicTicketsController(List<ThermodynamicTask> tasks) => (_tasks) = (tasks);

    public async Task RunAsync()
    {
        foreach (var ticket in _tasks)
        {
        }
    }
}
