using ParametricCombustionModel.Computation.BurningPropellantSolvers;
using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Computation.Test.Models;
using Xunit;

namespace ParametricCombustionModel.Computation.Test.Solvers;

public class InterPocketPropellantSolverTest
{
    [Fact]
    public void BaseCase()
    {
        // Arrange
        double pressure = 1.2e6;
        var burningParams = GetBurningParams();
        var solver = new InterPocketPropellantSolver(pressure,
                                                     TestPropellant.GetPropellantParams(),
                                                     TestPropellant.GetInterPocketThermodynamicParams());

        // Act
        var burningRate = solver.GetBurningRate(ref burningParams, out double surfaceTemperature);

        // Assert
        Assert.Equal(0.031640, Math.Round(burningRate, 6));
    }

    private static BurningParams GetBurningParams() =>
        new BurningParams
        {
            A_decompose = 58036521.6181994,
            E_decompose = 76587.30796555843,
            A_kinetic_flame_homoprop = 2229125453.596331,
            E_kinetic_flame_homoprop = 60387.546853299806,
            Nu = 1.3856,
            H_metal_burning = 974317755.6472487,
            E_metal_burning = 290329.1036386116,
            Delta_H = 21329733.980807018,
            K_diffusion_height = 1.0
        };
}
