using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Units;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Utilization of Double

/// <summary>
/// Represents the parameters related to the burn process using native double values.
/// </summary>
public readonly ref struct CombustionSolverParamsByDoubles
{
    /// <summary>
    /// Decomposition rate constant (pre-exponential factor).
    /// Measured in kg/(m^2*s).
    /// </summary>
    public required double ADecompose { get; init; }

    /// <summary>
    /// Activation energy for decomposition.
    /// Measured in J/mol.
    /// </summary>
    public required double EDecompose { get; init; }

    /// <summary>
    /// Frequency factor for the kinetic flame inter-pocket.
    /// Measured in 1/s.
    /// </summary>
    public required double AKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Activation energy for the kinetic flame inter-pocket.
    /// Measured in J/mol.
    /// </summary>
    public required double EKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Frequency factor for the kinetic flame pocket out skeleton.
    /// Measured in 1/s.
    /// </summary>
    public required double AKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Activation energy for the kinetic flame pocket out skeleton.
    /// Measured in J/mol.
    /// </summary>
    public required double EKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Frequency factor for the kinetic flame pocket skeleton.
    /// Measured in 1/s.
    /// </summary>
    public required double AKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Activation energy for the kinetic flame pocket skeleton.
    /// Measured in J/mol.
    /// </summary>
    public required double EKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Order of chemical reactions in kinetic flames at the inter pocket region.
    /// </summary>
    public required double NuInterPocket { get; init; }
    
    public required double NuPocketOutSkeleton { get; init; }
    
    public required double NuPocketSkeleton { get; init; }
    
    public required double AMetalBurningConstant { get; init; }

    public required double BMetalBurningConstant { get; init; }

    /// <summary>
    /// Specific energy change of the binder.
    /// Measured in J/kg.
    /// </summary>
    public required double DeltaH { get; init; }

    /// <summary>
    /// Diffusion height coefficient.
    /// </summary>
    public required double KDiffusionHeight { get; init; }

    public required double APowOrder { get; init; }

    public required double BPowOrder { get; init; }

    public required double KCoefficientRadiationTemperature { get; init; }

    /// <summary>
    /// Diffusion-flame standoff pressure-factor coefficient C_rxn ≥ 0 (shared across compositions).
    /// The diffusion standoff is scaled by [1 + C_rxn·f_c·(p_ref/p)^n_p] (p_ref = 7 MPa): with C_rxn = 0 it
    /// reduces exactly to the original laminar form, while C_rxn &gt; 0 gives coarse-AP-rich compositions
    /// (large f_c) a steeper burn-rate pressure exponent (BDP / Lengellé).
    /// See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KDiffusionPressureFactor { get; init; }

    /// <summary>
    /// Diffusion-flame standoff size-exponent m ≥ 0 (shared across compositions). In the two-mode
    /// petite-ensemble diffusion model each AP size mode has its own standoff scaled by
    /// [1 + C_rxn·(d/d_ref)^m·(p_ref/p)^n_p] with d_ref = 100 µm: m &gt; 0 makes coarse modes more
    /// pressure-sensitive (steeper ν) than fine modes. The exact size exponent is left free because
    /// BDP 1970 / Lengellé 2000 give only a range. See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KDiffusionSizeExponent { get; init; }

    /// <summary>
    /// Bimodal-packing heat-feedback enhancement K_pack ≥ 0 (shared across compositions). The total
    /// pocket heat flux to the surface is scaled by [1 + K_pack·f_c·(1−f_c)] (§11.1.1): the bimodality
    /// measure f_c·(1−f_c) is zero for monomodal AP (pure coarse or pure fine) and maximal for a balanced
    /// blend, so the factor selectively raises the burn-rate magnitude of bimodal compositions (dense
    /// bimodal packing → more intense surface heat feedback; Miller 1982, Kubota). K_pack = 0 ⇒ no change.
    /// See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KBimodalPackingFactor { get; init; }

    /// <summary>
    /// Diffusion-flame standoff pressure-exponent n_p ∈ [2,4] (shared across compositions). Each AP size
    /// mode's standoff reaction/turbulent term scales as [1 + C_rxn·(d/d_ref)^m·(p_ref/p)^n_p]: n_p = 2
    /// reproduces the original fixed quadratic pressure law, while n_p &gt; 2 sharpens how steeply the
    /// standoff contracts with pressure and hence the pure-mode burn-rate pressure exponents — the
    /// turbulent/chemical standoff exponent is regime-dependent, not universally 2 (Lengellé–Duterque–Trubert
    /// 2000; BDP 1970). See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KDiffusionPressureExponent { get; init; }

    /// <summary>
    /// WSB condensed-phase reaction coefficient K_wsb ≥ 0 (shared across compositions). The condensed-phase
    /// exothermic reaction supplies part of the sensible enthalpy, so the net surface heat demand becomes
    /// ṁ·[c_s·(T_s−T_0)·(1−θ) + ΔH] with completeness θ = Da/(1+Da), Da = K_wsb·(p/p_ref)² and p_ref = 1 MPa:
    /// K_wsb = 0 reproduces the original surface energy balance exactly, while K_wsb &gt; 0 lets the burn rate
    /// keep climbing once the kinetic flames go surface-attached at high pressure (de-saturates the 4–6.5 MPa
    /// tail). The p² Damköhler is the WSB / Zenin second-order gas-condensed coupling (Ward–Son–Brewster 1998,
    /// Combust. Flame 114:556; Zenin 1995, J. Propul. Power 11:752). See docs/research/wsb_condensed_phase_closure.md.
    /// </summary>
    public required double KCondensedReactionFactor { get; init; }

    /// <summary>
    /// AP self-deflagration (monopropellant premixed-flame) conductance K_AP ≥ 0 (shared across compositions),
    /// units W·m⁻²·K⁻¹ (lumped λ_g/δ_AP). Adds a parallel near-surface heat flux
    /// q_AP = K_AP·(1−f_c)·max(0, T_AP−T_s)·max(0, (p/p_dl)^n_AP − 1), T_AP = 1400 K fixed, n_AP fitted (vector[24]),
    /// p_dl = 2 MPa the fixed AP deflagration limit (Boggs 1970). Fine AP self-deflagrates as a premixed flame
    /// that — unlike the surface-attached diffusion/kinetic flames — does NOT saturate with pressure, de-saturating
    /// the 4–6.5 MPa burn rate of fine-AP-dominated compositions (pure-fine Bas_3). Weighted by the fine-AP surface
    /// fraction (1−f_c): coarse AP (f_c=1) gets nothing, pure-fine (f_c=0) gets the full term. K_AP = 0 reproduces the
    /// Step-6/8 model exactly (cannot regress). See docs/research/ap_monopropellant_premixed_flame.md §7.
    /// </summary>
    public required double KApPremixedFactor { get; init; }

    /// <summary>
    /// AP self-deflagration (monopropellant premixed-flame) pressure exponent n_AP ≥ 0.77 (SHARED across compositions),
    /// dimensionless. Sets the steepness of the deflagration-limit gate max(0, (p/p_dl)^n_AP − 1) in q_AP. The floor
    /// 0.77 is the Guirao &amp; Williams (1971) average over 20–100 atm and reproduces the fixed-exponent Step-7 model;
    /// a larger n_AP concentrates the de-saturating heat feedback in the high-pressure tail (6.5 MPa) where the
    /// fine-AP residual lives, without lifting the mid-range (Boggs 1970; Price 1984 — AP burn-rate slope is
    /// regime-dependent, not a single power law). See docs/research/ap_monopropellant_premixed_flame.md.
    /// </summary>
    public required double KApPressureExponent { get; init; }

    /// <summary>
    /// Bimodal-packing heat-feedback pressure-decay exponent a_pack ≥ 0 (SHARED across compositions), dimensionless.
    /// Multiplies the bimodal-packing factor by (p_ref/p)^a_pack, p_ref = 1 MPa, so the enhancement decays with
    /// pressure: the bimodal leading-edge flame (LEF) structure feeds back heat strongly only while the diffusion
    /// flames stand off (low pressure) and loses relative importance as they collapse to the surface at high pressure
    /// (Beckstead–Derr–Price 1970; AP plateau / particle-size literature). This lifts the low-pressure magnitude of the
    /// bimodal compositions (under-predicted Bas_2 at 1 MPa) and relaxes the 4 MPa over-prediction — a slope correction
    /// the pressure-independent K_pack cannot make. a_pack = 0 reproduces the pressure-independent factor exactly
    /// (cannot regress); the term is identically 0 for monomodal Bas_3/Bas_4 (f_c·(1−f_c)=0).
    /// See docs/research/bimodal_packing_pressure_decay.md.
    /// </summary>
    public required double KBimodalPackingPressureExponent { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CombustionSolverParamsByDoubles FromVector(
        ReadOnlySpan<double> vector)
    {
        return new CombustionSolverParamsByDoubles
        {
            ADecompose = vector[0],
            EDecompose = vector[1],
            AKineticFlameInterPocket = vector[2],
            EKineticFlameInterPocket = vector[3],
            AKineticFlamePocketOutSkeleton = vector[4],
            EKineticFlamePocketOutSkeleton = vector[5],
            AKineticFlamePocketSkeleton = vector[6],
            EKineticFlamePocketSkeleton = vector[7],
            NuInterPocket = vector[8],
            NuPocketOutSkeleton = vector[9],
            NuPocketSkeleton = vector[10],
            AMetalBurningConstant = vector[11],
            BMetalBurningConstant = vector[12],
            DeltaH = vector[13],
            KDiffusionHeight = vector[14],
            APowOrder = vector[15],
            BPowOrder = vector[16],
            KCoefficientRadiationTemperature = vector[17],
            KDiffusionPressureFactor = vector[18],
            KDiffusionSizeExponent = vector[19],
            KBimodalPackingFactor = vector[20],
            KDiffusionPressureExponent = vector[21],
            KCondensedReactionFactor = vector[22],
            KApPremixedFactor = vector[23],
            KApPressureExponent = vector[24],
            KBimodalPackingPressureExponent = vector[25]
        };
    }
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents the parameters related to the burn process in units.
/// </summary>
public readonly ref struct CombustionSolverParamsByUnits
{
    /// <summary>
    /// Gets the decomposition rate constant (pre-exponential factor in the Arrhenius equation for calculating the binder decomposition rate).
    /// Measured in kg/(m^2*s).
    /// </summary>
    public required MassFlux ADecompose { get; init; }

    /// <summary>
    /// Gets the activation energy for decomposition (activation energy in the Arrhenius equation for calculating the binder decomposition rate).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EDecompose { get; init; }

    /// <summary>
    /// Gets the frequency factor for the kinetic flame inter-pocket (pre-exponential factor in the Arrhenius equation for calculating the chemical reaction rates in the flame).
    /// Measured in 1/s.
    /// </summary>
    public required Frequency AKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Gets the activation energy for the kinetic flame inter-pocket (activation energy in the Arrhenius equation for calculating the chemical reaction rates in the flame).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Gets the frequency factor for the kinetic flame pocket out skeleton (pre-exponential factor in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket but not within the skeleton layer).
    /// Measured in 1/s.
    /// </summary>
    public required Frequency AKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Gets the activation energy for the kinetic flame pocket out skeleton (activation energy in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket but not within the skeleton layer).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Gets the frequency factor for the kinetic flame pocket skeleton (pre-exponential factor in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket and within the skeleton layer).
    /// Measured in 1/s.
    /// </summary>
    public required Frequency AKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Gets the activation energy for the kinetic flame pocket skeleton (activation energy in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket and within the skeleton layer).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Gets the order of chemical reactions in kinetic flames at the inter pocket region.
    /// </summary>
    public required double NuInterPocket { get; init; }
    
    public required double NuPocketOutSkeleton { get; init; }
    
    public required double NuPocketSkeleton { get; init; }

    public required AMetalBurningConstant AMetalBurningConstant { get; init; }

    public required BMetalBurningConstant BMetalBurningConstant { get; init; }

    /// <summary>
    /// Gets the specific energy change of the binder (difference between the heat of sublimation and the heat of binder decomposition).
    /// Measured in J/kg.
    /// </summary>
    public required SpecificEnergy DeltaH { get; init; }

    /// <summary>
    /// Gets the diffusion height coefficient.
    /// </summary>
    public required double KDiffusionHeight { get; init; }

    public required double APowOrder { get; init; }

    public required double BPowOrder { get; init; }

    public required double KCoefficientRadiationTemperature { get; init; }

    /// <summary>
    /// Diffusion-flame standoff pressure-factor coefficient C_rxn ≥ 0 (shared across compositions).
    /// The diffusion standoff is scaled by [1 + C_rxn·f_c·(p_ref/p)^n_p] (p_ref = 7 MPa): with C_rxn = 0 it
    /// reduces exactly to the original laminar form, while C_rxn &gt; 0 gives coarse-AP-rich compositions
    /// (large f_c) a steeper burn-rate pressure exponent (BDP / Lengellé).
    /// See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KDiffusionPressureFactor { get; init; }

    /// <summary>
    /// Diffusion-flame standoff size-exponent m ≥ 0 (shared across compositions). In the two-mode
    /// petite-ensemble diffusion model each AP size mode has its own standoff scaled by
    /// [1 + C_rxn·(d/d_ref)^m·(p_ref/p)^n_p] with d_ref = 100 µm: m &gt; 0 makes coarse modes more
    /// pressure-sensitive (steeper ν) than fine modes. The exact size exponent is left free because
    /// BDP 1970 / Lengellé 2000 give only a range. See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KDiffusionSizeExponent { get; init; }

    /// <summary>
    /// Bimodal-packing heat-feedback enhancement K_pack ≥ 0 (shared across compositions). The total
    /// pocket heat flux to the surface is scaled by [1 + K_pack·f_c·(1−f_c)] (§11.1.1): the bimodality
    /// measure f_c·(1−f_c) is zero for monomodal AP (pure coarse or pure fine) and maximal for a balanced
    /// blend, so the factor selectively raises the burn-rate magnitude of bimodal compositions (dense
    /// bimodal packing → more intense surface heat feedback; Miller 1982, Kubota). K_pack = 0 ⇒ no change.
    /// See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KBimodalPackingFactor { get; init; }

    /// <summary>
    /// Diffusion-flame standoff pressure-exponent n_p ∈ [2,4] (shared across compositions). Each AP size
    /// mode's standoff reaction/turbulent term scales as [1 + C_rxn·(d/d_ref)^m·(p_ref/p)^n_p]: n_p = 2
    /// reproduces the original fixed quadratic pressure law, while n_p &gt; 2 sharpens how steeply the
    /// standoff contracts with pressure and hence the pure-mode burn-rate pressure exponents — the
    /// turbulent/chemical standoff exponent is regime-dependent, not universally 2 (Lengellé–Duterque–Trubert
    /// 2000; BDP 1970). See docs/research/ap_size_pressure_exponent.md.
    /// </summary>
    public required double KDiffusionPressureExponent { get; init; }

    /// <summary>
    /// WSB condensed-phase reaction coefficient K_wsb ≥ 0 (shared across compositions). The condensed-phase
    /// exothermic reaction supplies part of the sensible enthalpy, so the net surface heat demand becomes
    /// ṁ·[c_s·(T_s−T_0)·(1−θ) + ΔH] with completeness θ = Da/(1+Da), Da = K_wsb·(p/p_ref)² and p_ref = 1 MPa:
    /// K_wsb = 0 reproduces the original surface energy balance exactly, while K_wsb &gt; 0 lets the burn rate
    /// keep climbing once the kinetic flames go surface-attached at high pressure (de-saturates the 4–6.5 MPa
    /// tail). The p² Damköhler is the WSB / Zenin second-order gas-condensed coupling (Ward–Son–Brewster 1998,
    /// Combust. Flame 114:556; Zenin 1995, J. Propul. Power 11:752). See docs/research/wsb_condensed_phase_closure.md.
    /// </summary>
    public required double KCondensedReactionFactor { get; init; }

    /// <summary>
    /// AP self-deflagration (monopropellant premixed-flame) conductance K_AP ≥ 0 (shared across compositions),
    /// units W·m⁻²·K⁻¹ (lumped λ_g/δ_AP). Adds a parallel near-surface heat flux
    /// q_AP = K_AP·(1−f_c)·max(0, T_AP−T_s)·max(0, (p/p_dl)^n_AP − 1), T_AP = 1400 K fixed, n_AP fitted (vector[24]),
    /// p_dl = 2 MPa the fixed AP deflagration limit (Boggs 1970). Fine AP self-deflagrates as a premixed flame
    /// that — unlike the surface-attached diffusion/kinetic flames — does NOT saturate with pressure, de-saturating
    /// the 4–6.5 MPa burn rate of fine-AP-dominated compositions (pure-fine Bas_3). Weighted by the fine-AP surface
    /// fraction (1−f_c): coarse AP (f_c=1) gets nothing, pure-fine (f_c=0) gets the full term. K_AP = 0 reproduces the
    /// Step-6/8 model exactly (cannot regress). See docs/research/ap_monopropellant_premixed_flame.md §7.
    /// </summary>
    public required double KApPremixedFactor { get; init; }

    /// <summary>
    /// AP self-deflagration (monopropellant premixed-flame) pressure exponent n_AP ≥ 0.77 (SHARED across compositions),
    /// dimensionless. Sets the steepness of the deflagration-limit gate max(0, (p/p_dl)^n_AP − 1) in q_AP. The floor
    /// 0.77 is the Guirao &amp; Williams (1971) average over 20–100 atm and reproduces the fixed-exponent Step-7 model;
    /// a larger n_AP concentrates the de-saturating heat feedback in the high-pressure tail (6.5 MPa) where the
    /// fine-AP residual lives, without lifting the mid-range (Boggs 1970; Price 1984 — AP burn-rate slope is
    /// regime-dependent, not a single power law). See docs/research/ap_monopropellant_premixed_flame.md.
    /// </summary>
    public required double KApPressureExponent { get; init; }

    /// <summary>
    /// Bimodal-packing heat-feedback pressure-decay exponent a_pack ≥ 0 (SHARED across compositions), dimensionless.
    /// Multiplies the bimodal-packing factor by (p_ref/p)^a_pack, p_ref = 1 MPa, so the enhancement decays with
    /// pressure: the bimodal leading-edge flame (LEF) structure feeds back heat strongly only while the diffusion
    /// flames stand off (low pressure) and loses relative importance as they collapse to the surface at high pressure
    /// (Beckstead–Derr–Price 1970; AP plateau / particle-size literature). This lifts the low-pressure magnitude of the
    /// bimodal compositions (under-predicted Bas_2 at 1 MPa) and relaxes the 4 MPa over-prediction — a slope correction
    /// the pressure-independent K_pack cannot make. a_pack = 0 reproduces the pressure-independent factor exactly
    /// (cannot regress); the term is identically 0 for monomodal Bas_3/Bas_4 (f_c·(1−f_c)=0).
    /// See docs/research/bimodal_packing_pressure_decay.md.
    /// </summary>
    public required double KBimodalPackingPressureExponent { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CombustionSolverParamsByUnits FromVector(
        ReadOnlySpan<double> vector)
    {
        return new CombustionSolverParamsByUnits
        {
            ADecompose = MassFlux.FromKilogramsPerSecondPerSquareMeter(vector[0]),
            EDecompose = MolarEnergy.FromJoulesPerMole(vector[1]),
            AKineticFlameInterPocket = Frequency.FromPerSecond(vector[2]),
            EKineticFlameInterPocket = MolarEnergy.FromJoulesPerMole(vector[3]),
            AKineticFlamePocketOutSkeleton = Frequency.FromPerSecond(vector[4]),
            EKineticFlamePocketOutSkeleton = MolarEnergy.FromJoulesPerMole(vector[5]),
            AKineticFlamePocketSkeleton = Frequency.FromPerSecond(vector[6]),
            EKineticFlamePocketSkeleton = MolarEnergy.FromJoulesPerMole(vector[7]),
            NuInterPocket = vector[8],
            NuPocketOutSkeleton = vector[9],
            NuPocketSkeleton = vector[10],
            AMetalBurningConstant = AMetalBurningConstant.FromSquareMetersPerSecond(vector[11]),
            BMetalBurningConstant = BMetalBurningConstant.FromCubicMetersPerSquareSecond(vector[12]),
            DeltaH = SpecificEnergy.FromJoulesPerKilogram(vector[13]),
            KDiffusionHeight = vector[14],
            APowOrder = vector[15],
            BPowOrder = vector[16],
            KCoefficientRadiationTemperature = vector[17],
            KDiffusionPressureFactor = vector[18],
            KDiffusionSizeExponent = vector[19],
            KBimodalPackingFactor = vector[20],
            KDiffusionPressureExponent = vector[21],
            KCondensedReactionFactor = vector[22],
            KApPremixedFactor = vector[23],
            KApPressureExponent = vector[24],
            KBimodalPackingPressureExponent = vector[25]
        };
    }
}

#endregion
