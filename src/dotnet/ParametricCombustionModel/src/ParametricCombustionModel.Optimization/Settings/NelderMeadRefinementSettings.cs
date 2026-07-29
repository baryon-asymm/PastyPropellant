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

    /// <summary>
    /// Run an in-loop memetic Nelder–Mead refiner during the DE search.
    ///
    /// <para><b>Currently unavailable</b> — <see cref="Validate"/> rejects it. The adapter package that
    /// provides the refiner is built against DotNetDifferentialEvolution 4.0.0 and is binary-incompatible
    /// with the 5.x this project now references.</para>
    /// </summary>
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

        // The in-loop refiner is unavailable on DotNetDifferentialEvolution 5.x. Rejected here, at
        // configuration time, rather than left to fail where it actually breaks: the refiner sits behind a
        // package boundary that links and loads cleanly, so the fault would otherwise surface as a
        // MissingMethodException on the EveryNGenerations-th generation — hours into a run, with the search
        // state gone. Deleting this one statement is the whole re-enablement once an adapter built against
        // 5.x is published; see the PackageReference comment in the project file.
        if (MemeticInLoop)
            throw new InvalidOperationException(
                "MemeticInLoop is not available: DotNetNelderMead.DifferentialEvolution 1.0.0 is built "
                + "against DotNetDifferentialEvolution 4.0.0, whose ProblemContext.Population and "
                + $".PopulationFfValues were removed in 5.1.0. Enabling it would abort the search at "
                + $"generation {EveryNGenerations} with a MissingMethodException, not at startup. Use "
                + "FinalPolish, which goes through DotNetNelderMead directly and is unaffected, until an "
                + "adapter built against DotNetDifferentialEvolution 5.x is published.");

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
