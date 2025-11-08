namespace ParametricCombustionModel.Telemetry.Instruments;

public static class GCMeasurer
{
    public static long TotalMemory => GC.GetTotalMemory(false);

    public static double PauseTimePercentage => GC.GetGCMemoryInfo().PauseTimePercentage;

    public static int GetCollectionCount(int generation)
    {
        return GC.CollectionCount(generation);
    }
}
