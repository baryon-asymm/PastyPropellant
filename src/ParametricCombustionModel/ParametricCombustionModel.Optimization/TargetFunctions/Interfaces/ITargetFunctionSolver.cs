namespace ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;

public interface ITargetFunctionSolver
{
    public void RunTargetFunction(Span<double> x, Span<double> values);
    public void RunTargetFunction(ref BurningParams burningParams, Span<double> values);
}
