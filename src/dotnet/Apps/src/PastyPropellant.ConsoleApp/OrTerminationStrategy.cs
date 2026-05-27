using DotNetDifferentialEvolution.Models;
using DotNetDifferentialEvolution.TerminationStrategies.Interfaces;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp;

public class OrTerminationStrategy : ITerminationStrategy
{
    private readonly ITerminationStrategy[] _strategies;
    private bool _fired;

    public ITerminationStrategy? FiringStrategy { get; private set; }

    public OrTerminationStrategy(params ITerminationStrategy[] strategies)
    {
        _strategies = strategies;
    }

    public bool ShouldTerminate(Population population)
    {
        bool any = false;
        ITerminationStrategy? firstFired = null;

        // Evaluate every child so each maintains its internal state
        // (CustomStagnationStreakTerminationStrategy updates LastBest /
        // CurrentStreak on every call).
        foreach (var strategy in _strategies)
        {
            if (strategy.ShouldTerminate(population))
            {
                any = true;
                firstFired ??= strategy;
            }
        }

        if (any && !_fired)
        {
            _fired = true;
            FiringStrategy = firstFired;
            EventBus<InfoLogEvent>.Publish(new InfoLogEvent(
                Message: $"Termination triggered by {firstFired!.GetType().Name}",
                Sender: nameof(OrTerminationStrategy)));
        }

        return any;
    }
}
