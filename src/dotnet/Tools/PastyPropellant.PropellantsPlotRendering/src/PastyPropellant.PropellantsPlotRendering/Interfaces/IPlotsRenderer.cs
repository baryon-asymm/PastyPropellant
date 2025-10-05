using PastyPropellant.Core.Utils;
using PastyPropellant.PropellantsPlotRendering.Models;

namespace PastyPropellant.PropellantsPlotRendering.Interfaces;

public interface IPlotsRenderer
{
    public Task<OperationResult<PlotsResult>> RenderPlotsAsync(
        string propellantsFilePath);
}
