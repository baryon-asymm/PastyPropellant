namespace ParametricCombustionModel.PlotRenderer.Extensions;

public static class StreamExtensions
{
    public static void ResetToStart(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Position = 0;
    }
}
