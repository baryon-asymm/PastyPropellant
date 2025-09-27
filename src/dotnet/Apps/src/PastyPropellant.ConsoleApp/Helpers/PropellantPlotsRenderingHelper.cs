using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using PastyPropellant.PropellantsPlotRendering.Interfaces;
using PastyPropellant.PropellantsPlotRendering.Models;
using PastyPropellant.PropellantsPlotRendering.Renderers;

namespace PastyPropellant.ConsoleApp.Helpers;

public class PropellantPlotsRenderingHelper
{
    private readonly IPlotsRenderer _plotsRenderer;

    public PropellantPlotsRenderingHelper(string pyPlotsRenderingScriptPath)
    {
        _plotsRenderer = new PythonPlotsRenderer(pyPlotsRenderingScriptPath);
    }

    public async Task<OperationResult<PlotsResult>> RenderPlotsAsync(string propellantsFilePath)
    {
        try
        {
            return await TryRenderPlotsAsync(propellantsFilePath);
        }
        catch (Exception ex)
        {
            return new OperationResult<PlotsResult>(ex);
        }
    }

    private async Task<OperationResult<PlotsResult>> TryRenderPlotsAsync(string propellantsFilePath)
    {
        EventBus<InfoLogEvent>.Publish(new InfoLogEvent("Rendering plots...", nameof(PropellantPlotsRenderingHelper)));
        EventBus<ProcessInfoLogEvent>.Subscribe(RenderingProcessInfoLogEventHandler);

        var operationResult = await _plotsRenderer.RenderPlotsAsync(propellantsFilePath);

        EventBus<ProcessInfoLogEvent>.Unsubscribe(RenderingProcessInfoLogEventHandler);

        return operationResult;
    }

    private void RenderingProcessInfoLogEventHandler(ProcessInfoLogEvent logEvent)
    {
        EventBus<InfoLogEvent>.Publish(new InfoLogEvent(logEvent.Message, nameof(PythonPlotsRenderer)));
    }
}
