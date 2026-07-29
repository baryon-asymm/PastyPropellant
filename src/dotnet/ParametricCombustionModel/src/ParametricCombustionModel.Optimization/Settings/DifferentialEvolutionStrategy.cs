namespace ParametricCombustionModel.Optimization.Settings;

/// <summary>
/// Selects which Differential Evolution variant the optimiser uses (DotNetDifferentialEvolution 5.x).
/// </summary>
/// <remarks>
/// <see cref="Classic"/> and <see cref="Jde"/> consume
/// <see cref="DifferentialEvolutionSettings.MutationForce"/> /
/// <see cref="DifferentialEvolutionSettings.CrossoverProbability"/> (for jDE these are the *initial*
/// values it then self-adapts). <see cref="Jade"/> and <see cref="Shade"/> tune F and CR internally
/// and use <see cref="DifferentialEvolutionSettings.PBestRate"/> /
/// <see cref="DifferentialEvolutionSettings.ArchiveSizeRate"/> (+ <see cref="DifferentialEvolutionSettings.MemorySize"/>
/// for SHADE). <see cref="LShade"/> additionally requires
/// <see cref="DifferentialEvolutionSettings.MaxEvaluationNumber"/> because its linear population-size
/// reduction needs a fixed evaluation budget.
/// </remarks>
public enum DifferentialEvolutionStrategy
{
    /// <summary>DE/rand/1/bin with constant F/CR — the pre-2.0 default behaviour.</summary>
    Classic,

    /// <summary>jDE — per-individual self-adapting F/CR (Brest et al., 2006). Fixed population.</summary>
    Jde,

    /// <summary>JADE — DE/current-to-pbest/1 with archive and adaptive F/CR (Zhang &amp; Sanderson, 2009). Fixed population.</summary>
    Jade,

    /// <summary>SHADE — JADE with success-history parameter adaptation (Tanabe &amp; Fukunaga, 2013). Fixed population.</summary>
    Shade,

    /// <summary>L-SHADE — SHADE with linear population-size reduction (CEC-2014). Requires a fixed evaluation budget.</summary>
    LShade
}
