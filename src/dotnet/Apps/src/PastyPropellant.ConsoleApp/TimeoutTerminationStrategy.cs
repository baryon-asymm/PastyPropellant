using System.Diagnostics;
using DotNetDifferentialEvolution.Models;
using DotNetDifferentialEvolution.TerminationStrategies.Interfaces;

namespace PastyPropellant.ConsoleApp;

public class TimeoutTerminationStrategy : ITerminationStrategy
{
    private readonly Stopwatch _stopwatch;
    private readonly TimeSpan _timeout;

    public TimeoutTerminationStrategy(TimeSpan timeout)
    {
        _timeout = timeout;
        _stopwatch = Stopwatch.StartNew();
    }

    public bool ShouldTerminate(Population population)
    {
        return _stopwatch.Elapsed >= _timeout;
    }
}
