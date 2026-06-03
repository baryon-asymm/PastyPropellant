namespace ParametricCombustionModel.Optimization.Settings;

/// <summary>
/// Configures the optional Nelder–Mead local-search refinement layered on top of the global DE search
/// (DotNetNelderMead.DifferentialEvolution). Two independent stages can be enabled:
/// <list type="bullet">
/// <item>an in-loop <b>memetic</b> refiner that polishes the current best individual every
/// <see cref="EveryNGenerations"/> generations (<c>WithNelderMeadLocalSearch</c>);</item>
/// <item>a <b>final</b> sequential DE→Nelder–Mead handoff that polishes the converged best once.</item>
/// </list>
/// The refiner reuses the optimiser's own <see cref="DotNetOptimization.Abstractions.IFitnessFunctionEvaluator"/>,
/// so it minimises exactly the same aggregated fitness (+ penalties) as the DE search.
/// </summary>
public sealed record NelderMeadRefinementSettings
{
    /// <summary>Master switch. When <c>false</c> the optimiser behaves exactly as plain DE.</summary>
    public bool Enabled { get; init; }

    /// <summary>Run an in-loop memetic Nelder–Mead refiner during the DE search.</summary>
    public bool MemeticInLoop { get; init; }

    /// <summary>Generation interval between memetic refiner calls. Used only when <see cref="MemeticInLoop"/> is set.</summary>
    public int EveryNGenerations { get; init; } = 25;

    /// <summary>Per-call Nelder–Mead evaluation budget for the memetic refiner.</summary>
    public long MemeticMaxEvaluationsPerCall { get; init; } = 200;

    /// <summary>Run a final sequential Nelder–Mead polish of the DE best after the search completes.</summary>
    public bool FinalPolish { get; init; }

    /// <summary>Total Nelder–Mead evaluation budget for the final polish.</summary>
    public long FinalPolishMaxEvaluations { get; init; } = 20_000;

    /// <summary>Use the dimension-adaptive (Gao–Han 2012) simplex coefficients instead of the classic 1965 ones.</summary>
    public bool AdaptiveCoefficients { get; init; } = true;

    /// <summary>Simplex domain-size convergence tolerance.</summary>
    public double DomainTolerance { get; init; } = 1e-8;

    /// <summary>Vertex value-spread convergence tolerance.</summary>
    public double FunctionTolerance { get; init; } = 1e-8;

    /// <summary>Maximum automatic simplex restarts on convergence before stopping.</summary>
    public int Restarts { get; init; } = 2;

    /// <summary>Refinement turned off — the optimiser runs plain DE.</summary>
    public static NelderMeadRefinementSettings Disabled { get; } = new() { Enabled = false };

    /// <summary>Throws when an enabled configuration carries nonsensical values.</summary>
    public void Validate()
    {
        if (!Enabled)
            return;

        if (!MemeticInLoop && !FinalPolish)
            throw new InvalidOperationException(
                "Nelder–Mead refinement is enabled but neither MemeticInLoop nor FinalPolish is selected.");

        if (MemeticInLoop)
        {
            if (EveryNGenerations <= 0)
                throw new InvalidOperationException("EveryNGenerations must be positive when MemeticInLoop is enabled.");
            if (MemeticMaxEvaluationsPerCall <= 0)
                throw new InvalidOperationException("MemeticMaxEvaluationsPerCall must be positive when MemeticInLoop is enabled.");
        }

        if (FinalPolish && FinalPolishMaxEvaluations <= 0)
            throw new InvalidOperationException("FinalPolishMaxEvaluations must be positive when FinalPolish is enabled.");

        if (DomainTolerance < 0 || FunctionTolerance < 0)
            throw new InvalidOperationException("Convergence tolerances must be non-negative.");

        if (Restarts < 0)
            throw new InvalidOperationException("Restarts must be non-negative.");
    }
}
