using UnitsNet;

namespace ParametricCombustionModel.Test.Share.Helpers;

public static class PressureHelper
{
    public static Pressure MinPressure => Pressure.FromAtmospheres(10);
    public static Pressure MaxPressure => Pressure.FromAtmospheres(65);

    public static IEnumerable<Pressure> GetPressures()
    {
        const int count = 100;

        var step = (MaxPressure.Atmospheres - MinPressure.Atmospheres) / count;
        return Enumerable.Range(0, count)
                         .Select(i => MinPressure + Pressure.FromAtmospheres(step * i));
    }
}
