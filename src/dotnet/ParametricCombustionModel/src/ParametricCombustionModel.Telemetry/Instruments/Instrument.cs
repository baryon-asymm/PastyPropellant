namespace ParametricCombustionModel.Telemetry.Instruments;

public abstract class Instrument
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string Unit { get; init; }
    public string Path { get; init; }

    public Instrument(string name, string description, string unit, string path)
    {
        Name = name;
        Description = description;
        Unit = unit;
        Path = path;
    }
}
