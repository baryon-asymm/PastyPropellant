using PastyPropellant.ConsoleApp.Interfaces;
using PastyPropellant.Core.Models.Thermodynamic;

namespace PastyPropellant.ConsoleApp.Controllers.Thermodynamic;

public record ThermodynamicTask(
    ThermodynamicTicket Ticket,
    IPossibleCombustionProductsHandler CombustionProductsHandler
);
