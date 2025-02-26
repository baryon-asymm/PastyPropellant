namespace ParametricCombustionModel.Telemetry.GCMetricsRecorders;

public static class GCCounter
{
    public static long TotalMemory => GC.GetTotalMemory(false);

    public static double PauseTimePercentage => GC.GetGCMemoryInfo().PauseTimePercentage;

    public static int GetCollectionCount(int generation)
    {
        return GC.CollectionCount(generation);
    }
}
