using DotNetDifferentialEvolution.Models;
using DotNetDifferentialEvolution.TerminationStrategies;
using DotNetDifferentialEvolution.TerminationStrategies.Interfaces;

namespace PastyPropellant.ConsoleApp;

public class CustomStagnationStreakTerminationStrategy : ITerminationStrategy
{
    public int MaxStagnationStreak { get; init; }

    public double StagnationThreshold { get; init; }

    public int CurrentStagnationStreak { get; private set; }

    public double LastBestFitnessFunctionValue { get; private set; } = double.MinValue;

    public CustomStagnationStreakTerminationStrategy(int maxStagnationStreak, double stagnationThreshold)
    {
        MaxStagnationStreak = maxStagnationStreak;
        StagnationThreshold = stagnationThreshold;
    }

    public bool ShouldTerminate(Population population)
    {
        population.MoveCursorToBestIndividual();
        if (population.IndividualCursor.FitnessFunctionValue >= 1e3)
        {
            return false;
        }

        if (Math.Abs(population.IndividualCursor.FitnessFunctionValue - LastBestFitnessFunctionValue) > StagnationThreshold)
        {
            LastBestFitnessFunctionValue = population.IndividualCursor.FitnessFunctionValue;
            CurrentStagnationStreak = 0;
        }
        else
        {
            CurrentStagnationStreak++;
        }

        return CurrentStagnationStreak >= MaxStagnationStreak;
    }
}