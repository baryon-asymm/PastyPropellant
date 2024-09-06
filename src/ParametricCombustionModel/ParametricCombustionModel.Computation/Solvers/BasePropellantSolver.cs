using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Common;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Solvers;

/// <summary>
/// Represents the base class for solving propellant combustion problems in rocket engines.
/// This abstract class provides a framework for calculating the surface temperature and burn rate of the propellant,
/// and defines abstract methods that must be implemented by derived classes to calculate specific parameters related to
/// the combustion process.
/// </summary>
/// <remarks>
/// Derived classes are expected to provide concrete implementations for methods that calculate specific parameters such as
/// the error in surface heat fluxes, the decomposition rate, and other key metrics needed for accurately modeling the combustion process.
/// This class serves as a foundation for more specialized solvers that address various aspects of propellant combustion.
/// </remarks>
public abstract class BasePropellantSolver : ISolverVisitor
{
#region Properties

    /// <summary>
    /// The minimum surface temperature for the propellant combustion process.
    /// This property is used as the lower bound in binary search algorithms for solving the transcendental equation
    /// to find the surface temperature of the propellant (condensed phase).
    /// </summary>
    public static Temperature MinSurfaceTemperature { get; set; } = Temperature.FromKelvins(100);

    /// <summary>
    /// The maximum surface temperature for the propellant combustion process.
    /// This property is used as the upper bound in binary search algorithms for solving the transcendental equation
    /// to find the surface temperature of the propellant (condensed phase).
    /// </summary>
    public static Temperature MaxSurfaceTemperature { get; set; } = Temperature.FromKelvins(5000);

#endregion

#region Abstracts

    /// <summary>
    /// Accepts a visitor to process combustion solver parameters and problem context using units.
    /// This method must be implemented by derived classes to provide specific behavior for handling the visitor pattern.
    /// </summary>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParams"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByUnits"/>.</param>
    public abstract void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context);

    /// <summary>
    /// Calculates the error in surface heat fluxes for the given pressure, surface temperature, and burn parameters.
    /// This method is utilized within the binary search algorithm to determine the surface temperature of the propellant by evaluating 
    /// how well the current temperature matches the desired thermal equilibrium condition.
    /// </summary>
    /// <param name="surfaceTemperature">The surface temperature of the propellant (temperature of the condensed phase) for which the error in heat fluxes is calculated.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including factors such as reaction kinetics and activation energy, provided as <see cref="CombustionSolverParams"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByUnits"/>.</param>
    /// <returns>
    /// The error in surface heat fluxes as a <see cref="HeatFlux"/>. This value represents the difference between the calculated and expected heat fluxes
    /// and is used to guide the binary search algorithm in locating the equilibrium surface temperature.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected abstract HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context);

    /// <summary>
    /// Accepts a visitor to process combustion solver parameters and problem context using doubles.
    /// This method must be implemented by derived classes to provide specific behavior for handling the visitor pattern.
    /// </summary>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByDoubles"/>.</param>
    public abstract void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context);

    /// <summary>
    /// Calculates the error in surface heat fluxes for the given pressure, surface temperature, and burn parameters.
    /// This method is utilized within the binary search algorithm to determine the surface temperature of the propellant by evaluating 
    /// how well the current temperature matches the desired thermal equilibrium condition.
    /// </summary>
    /// <param name="surfaceTemperature">The surface temperature of the propellant (temperature of the condensed phase) as a double.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including factors such as reaction kinetics and activation energy, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByDoubles"/>.</param>
    /// <returns>
    /// The error in surface heat fluxes as a double. This value represents the difference between the calculated and expected heat fluxes
    /// and is used to guide the binary search algorithm in locating the equilibrium surface temperature.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected abstract double GetSurfaceHeatFluxesError(
        double surfaceTemperature,
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context);

#endregion

#region Computation Methods

    /// <summary>
    /// Attempts to find the surface temperature of the propellant using a binary search algorithm
    /// within the predefined temperature range from <see cref="MinSurfaceTemperature"/> to <see cref="MaxSurfaceTemperature"/>.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, which influences the equilibrium temperature.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including factors like reaction kinetics and activation energy, provided as <see cref="CombustionSolverParams"/>.</param>
    /// <param name="surfaceTemperature">
    /// When this method returns, contains the surface temperature of the propellant, if a valid temperature
    /// is found within the defined range and tolerance; otherwise, it contains a temperature value less than or equal to zero.
    /// </param>
    /// <returns>
    /// <c>true</c> if a suitable surface temperature is found within the range defined by
    /// <see cref="MinSurfaceTemperature"/> and <see cref="MaxSurfaceTemperature"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method uses a binary search approach to determine the surface temperature of the propellant,
    /// ensuring that the returned temperature is in thermal equilibrium given the pressure conditions in the combustion chamber.
    /// The search is conducted within the bounds defined by the properties <see cref="MinSurfaceTemperature"/>
    /// and <see cref="MaxSurfaceTemperature"/>. The method returns <c>false</c> if no valid temperature is found within these bounds.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual bool TryGetSurfaceTemperature(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context,
        out Temperature surfaceTemperature)
    {
        var leftTemperature = MinSurfaceTemperature;
        var rightTemperature = MaxSurfaceTemperature;
        var tolerance = TemperatureDelta.FromKelvins(1e-8);

        surfaceTemperature = GetSurfaceTemperatureByBinarySearchOrFail(solverParams,
                                                                       context,
                                                                       ref leftTemperature,
                                                                       ref rightTemperature,
                                                                       tolerance);

        return surfaceTemperature > Temperature.Zero;
    }

    /// <summary>
    /// Calculates the burn rate of the propellant based on the given decomposition rate and propellant parameters.
    /// The burn rate is determined by dividing the decomposition rate by the propellant density.
    /// </summary>
    /// <param name="decomposeRate">The rate at which the propellant decomposes, represented as a <see cref="MassFlux"/> (kg/(m²·s)).</param>
    /// <param name="propellantParams">A reference to the parameters related to the propellant, including its density, provided as <see cref="PropellantParams"/>.</param>
    /// <returns>
    /// The burn rate as a <see cref="Speed"/>, representing the velocity at which the propellant's surface recedes (m/s).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual Speed GetBurnRate(
        in MassFlux decomposeRate,
        in PropellantParams propellantParams) =>
        decomposeRate / propellantParams.Density;

    /// <summary>
    /// Calculates the decomposition rate of the propellant based on the given surface temperature and burn parameters 
    /// using the Arrhenius equation.
    /// </summary>
    /// <param name="surfaceTemperature">The surface temperature of the propellant (temperature of the condensed phase), provided as a <see cref="Temperature"/>.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParams"/>.</param>
    /// <returns>
    /// The decomposition rate as a <see cref="MassFlux"/>, representing the mass flux of decomposed material 
    /// per unit area per unit time (kg/(m²·s)).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual MassFlux GetDecomposeRate(
        in Temperature surfaceTemperature,
        in CombustionSolverParams solverParams)
    {
        var aDecompose = solverParams.ADecompose;
        var eDecompose = solverParams.EDecompose;
        const double gasConstant = PhysicalConstants.UniversalGasConstant; // J/(mol*K)

        var molarEnergy = MolarEnergy.FromJoulesPerMole(gasConstant * surfaceTemperature.Kelvins);

        return aDecompose * Math.Exp(-eDecompose / molarEnergy);
    }

#endregion

#region Search The Surface Temperature

    /// <summary>
    /// Finds the surface temperature of the propellant using a binary search algorithm
    /// within the specified temperature bounds. The method attempts to solve for the temperature
    /// where the heat flux error function crosses zero, indicating thermal equilibrium.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, influencing the equilibrium temperature.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including reaction kinetics and activation energy, provided as <see cref="CombustionSolverParams"/>.</param>
    /// <param name="leftTemperature">
    /// A reference to the lower bound of the temperature range for the binary search. 
    /// This value is updated during the search process to reflect the current search interval.
    /// </param>
    /// <param name="rightTemperature">
    /// A reference to the upper bound of the temperature range for the binary search. 
    /// This value is updated during the search process to reflect the current search interval.
    /// </param>
    /// <param name="tolerance">The precision of the search. The method will continue searching until the temperature difference is within this tolerance.</param>
    /// <returns>
    /// The surface temperature of the propellant, as a <see cref="Temperature"/>, if a solution is found within the specified tolerance.
    /// Returns a special value of -1 Kelvin if no suitable temperature can be determined within the bounds, indicating that no solution exists.
    /// </returns>
    /// <remarks>
    /// The method performs a binary search by repeatedly narrowing the range between <paramref name="leftTemperature"/> and <paramref name="rightTemperature"/>.
    /// It evaluates the heat flux error at the current bounds and midpoint. If the error signs at the bounds are opposite, the solution lies within the range.
    /// If the product of the errors at the bounds is positive, it indicates that no solution exists within the specified range, and the method returns -1 Kelvin.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Temperature GetSurfaceTemperatureByBinarySearchOrFail(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context,
        ref Temperature leftTemperature,
        ref Temperature rightTemperature,
        in TemperatureDelta tolerance)
    {
        if (leftTemperature > rightTemperature)
            (leftTemperature, rightTemperature) = (rightTemperature, leftTemperature);

        var leftValue = GetSurfaceHeatFluxesError(leftTemperature, solverParams, context);
        var rightValue = GetSurfaceHeatFluxesError(rightTemperature, solverParams, context);

        const double greaterThisNotExistSolution = 0.0;
        const double unavailableSurfaceTemperature = -1.0;
        if (leftValue.WattsPerSquareMeter * rightValue.WattsPerSquareMeter > greaterThisNotExistSolution)
            return Temperature.FromKelvins(unavailableSurfaceTemperature);

        var meanTemperatureDouble = (leftTemperature.Kelvins + rightTemperature.Kelvins) / 2.0;
        var meanTemperature = Temperature.FromKelvins(meanTemperatureDouble);
        while ((rightTemperature - leftTemperature) > tolerance)
        {
            var middleValue = GetSurfaceHeatFluxesError(meanTemperature, solverParams, context);

            if (middleValue.WattsPerSquareMeter * leftValue.WattsPerSquareMeter < 0.0)
            {
                rightTemperature = meanTemperature;
                // rightValue = middleValue;
            }
            else
            {
                leftTemperature = meanTemperature;
                leftValue = middleValue;
            }

            meanTemperatureDouble = (leftTemperature.Kelvins + rightTemperature.Kelvins) / 2.0;
            meanTemperature = Temperature.FromKelvins(meanTemperatureDouble);
        }

        return meanTemperature;
    }

#endregion

#region Computation Methods with Double Parameters

    /// <summary>
    /// Attempts to find the surface temperature of the propellant using a binary search algorithm
    /// within the predefined temperature range from <see cref="MinSurfaceTemperature"/> to <see cref="MaxSurfaceTemperature"/>.
    /// </summary>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including factors like reaction kinetics and activation energy, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByDoubles"/>.</param>
    /// <param name="surfaceTemperature">
    /// When this method returns, contains the surface temperature of the propellant as a double, if a valid temperature
    /// is found within the defined range and tolerance; otherwise, it contains a value less than or equal to zero.
    /// </param>
    /// <returns>
    /// <c>true</c> if a suitable surface temperature is found within the range defined by
    /// <see cref="MinSurfaceTemperature"/> and <see cref="MaxSurfaceTemperature"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method uses a binary search approach to determine the surface temperature of the propellant,
    /// ensuring that the returned temperature is in thermal equilibrium given the pressure conditions in the combustion chamber.
    /// The search is conducted within the bounds defined by the properties <see cref="MinSurfaceTemperature"/>
    /// and <see cref="MaxSurfaceTemperature"/>. The method returns <c>false</c> if no valid temperature is found within these bounds.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual bool TryGetSurfaceTemperature(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context,
        out double surfaceTemperature)
    {
        var leftTemperature = MinSurfaceTemperature.Kelvins;
        var rightTemperature = MaxSurfaceTemperature.Kelvins;
        var tolerance = 1e-8;

        surfaceTemperature = GetSurfaceTemperatureByBinarySearchOrFail(solverParams,
                                                                       context,
                                                                       leftTemperature,
                                                                       rightTemperature,
                                                                       tolerance);

        return surfaceTemperature > 0.0;
    }

    /// <summary>
    /// Calculates the burn rate of the propellant based on the given decomposition rate and propellant parameters.
    /// The burn rate is determined by dividing the decomposition rate by the propellant density.
    /// </summary>
    /// <param name="decomposeRate">The rate at which the propellant decomposes, represented as a double (kg/(m²·s)).</param>
    /// <param name="propellantParams">A reference to the parameters related to the propellant, including its density, provided as <see cref="PropellantParamsByDoubles"/>.</param>
    /// <returns>
    /// The burn rate as a double, representing the velocity at which the propellant's surface recedes (m/s).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual double GetBurnRate(
        double decomposeRate,
        in PropellantParamsByDoubles propellantParams) =>
        decomposeRate / propellantParams.Density;

    /// <summary>
    /// Calculates the decomposition rate of the propellant based on the given surface temperature and burn parameters 
    /// using the Arrhenius equation.
    /// </summary>
    /// <param name="surfaceTemperature">The surface temperature of the propellant (temperature of the condensed phase) as a double.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <returns>
    /// The decomposition rate as a double, representing the mass flux of decomposed material 
    /// per unit area per unit time (kg/(m²·s)).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual double GetDecomposeRate(
        double surfaceTemperature,
        in CombustionSolverParamsByDoubles solverParams)
    {
        var aDecompose = solverParams.ADecompose;
        var eDecompose = solverParams.EDecompose;
        const double gasConstant = PhysicalConstants.UniversalGasConstant; // J/(mol*K)

        var molarEnergy = gasConstant * surfaceTemperature;

        return aDecompose * Math.Exp(-eDecompose / molarEnergy);
    }

#endregion

#region Search The Surface Temperature with Double Parameters

    /// <summary>
    /// Finds the surface temperature of the propellant using a binary search algorithm
    /// within the specified temperature bounds. The method attempts to solve for the temperature
    /// where the heat flux error function crosses zero, indicating thermal equilibrium.
    /// </summary>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including reaction kinetics and activation energy, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByDoubles"/>.</param>
    /// <param name="leftTemperature">
    /// A reference to the lower bound of the temperature range for the binary search. 
    /// This value is updated during the search process to reflect the current search interval.
    /// </param>
    /// <param name="rightTemperature">
    /// A reference to the upper bound of the temperature range for the binary search. 
    /// This value is updated during the search process to reflect the current search interval.
    /// </param>
    /// <param name="tolerance">The precision of the search. The method will continue searching until the temperature difference is within this tolerance.</param>
    /// <returns>
    /// The surface temperature of the propellant as a double if a solution is found within the specified tolerance.
    /// Returns a special value of -1 Kelvin if no suitable temperature can be determined within the bounds, indicating that no solution exists.
    /// </returns>
    /// <remarks>
    /// The method performs a binary search by repeatedly narrowing the range between <paramref name="leftTemperature"/> and <paramref name="rightTemperature"/>.
    /// It evaluates the heat flux error at the current bounds and midpoint. If the error signs at the bounds are opposite, the solution lies within the range.
    /// If the product of the errors at the bounds is positive, it indicates that no solution exists within the specified range, and the method returns -1 Kelvin.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetSurfaceTemperatureByBinarySearchOrFail(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context,
        double leftTemperature,
        double rightTemperature,
        double tolerance)
    {
        if (leftTemperature > rightTemperature)
            (leftTemperature, rightTemperature) = (rightTemperature, leftTemperature);

        var leftValue = GetSurfaceHeatFluxesError(leftTemperature, solverParams, context);
        var rightValue = GetSurfaceHeatFluxesError(rightTemperature, solverParams, context);

        const double greaterThisNotExistSolution = 0.0;
        const double unavailableSurfaceTemperature = -1.0;
        if (leftValue * rightValue > greaterThisNotExistSolution)
            return unavailableSurfaceTemperature;

        var meanTemperature = (leftTemperature + rightTemperature) / 2.0;
        while ((rightTemperature - leftTemperature) > tolerance)
        {
            var middleValue = GetSurfaceHeatFluxesError(meanTemperature, solverParams, context);

            if (middleValue * leftValue < 0.0)
            {
                rightTemperature = meanTemperature;
                // rightValue = middleValue;
            }
            else
            {
                leftTemperature = meanTemperature;
                leftValue = middleValue;
            }

            meanTemperature = (leftTemperature + rightTemperature) / 2.0;
        }

        return meanTemperature;
    }

#endregion
}
