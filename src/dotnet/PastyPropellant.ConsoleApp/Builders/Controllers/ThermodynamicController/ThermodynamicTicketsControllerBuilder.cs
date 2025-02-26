using PastyPropellant.ConsoleApp.Builders.ThermodynamicController;
using PastyPropellant.ConsoleApp.Controllers.Thermodynamic;
using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Models.Thermodynamic;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Builders.Controllers.ThermodynamicController;

public class ThermodynamicTicketsControllerBuilder
{
    private IModelReader<List<ThermodynamicSubstance>>? _substanceReader;
    private List<ThermodynamicSubstance> _substances;
    private IModelReader<List<ThermodynamicTicket>>? _ticketReader;

    private List<ThermodynamicTicket> _tickets;

    private ThermodynamicTicketsControllerBuilder()
    {
    }

    private async Task<OperationResult<ThermodynamicTaskController>> LoadTicketsAsync()
    {
        var operationResult = await _ticketReader.ReadAllAsync();
        if (operationResult.IsSuccess)
        {
            _tickets = operationResult.Value ?? throw new ArgumentNullException(nameof(_tickets));
            return await LoadSubstancesAsync();
        }

        return new OperationResult<ThermodynamicTaskController>(operationResult.Exception);
    }

    private async Task<OperationResult<ThermodynamicTaskController>> LoadSubstancesAsync()
    {
        var operationResult = await _substanceReader.ReadAllAsync();
        if (operationResult.IsSuccess)
        {
            _substances = operationResult.Value ?? throw new ArgumentNullException(nameof(_substances));
            return CreateThermodynamicTaskController();
        }

        return new OperationResult<ThermodynamicTaskController>(operationResult.Exception);
    }

    private OperationResult<ThermodynamicTaskController> CreateThermodynamicTaskController()
    {
        ReadOnlyMemory<ThermodynamicSubstance> substances = _substances.ToArray();
        var tasks = new List<ThermodynamicTask>();

        foreach (var ticket in _tickets)
        {
            var parser = new PropellantChemicalElementsParser() as IPossibleCombustionProductsFinder;
            var operationResult = parser.FindPossibleCombustionSubstances(substances, ticket.Formula);
            if (operationResult.IsSuccess == false)
                return new OperationResult<ThermodynamicTaskController>(operationResult.Exception);
            var handler = operationResult.Value ?? throw new ArgumentNullException(nameof(operationResult.Value));
            tasks.Add(new ThermodynamicTask(ticket, handler));
        }

        return new OperationResult<ThermodynamicTaskController>(
                                                                new ThermodynamicTaskController(tasks)
                                                               );
    }

    public ThermodynamicTicketsControllerBuilder WithTicketReader(IModelReader<List<ThermodynamicTicket>>? ticketReader)
    {
        _ticketReader = ticketReader;
        return this;
    }

    public ThermodynamicTicketsControllerBuilder WithSubstanceReader(
        IModelReader<List<ThermodynamicSubstance>>? substanceReader)
    {
        _substanceReader = substanceReader;
        return this;
    }

    public async Task<OperationResult<ThermodynamicTaskController>> BuildAsync()
    {
        if (_ticketReader is null || _substanceReader is null)
            throw new InvalidOperationException("Ticket reader and substance reader must be set");

        var operationResult = await LoadTicketsAsync();
        return operationResult;
    }

    public static ThermodynamicTicketsControllerBuilder Create()
    {
        return new ThermodynamicTicketsControllerBuilder();
    }
}
