using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Models.Thermodynamic;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Helpers.ThermodynamicTickets;

public class ThermodynamicTicketsHelper(
    IModelReader<List<ThermodynamicTicket>> modelReader
)
{
    public virtual Task<OperationResult<List<ThermodynamicTicket>>> GetAllAsync()
    {
        return modelReader.ReadAllAsync();
    }
}
