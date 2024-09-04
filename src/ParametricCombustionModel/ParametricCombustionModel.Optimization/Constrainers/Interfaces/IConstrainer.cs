namespace ParametricCombustionModel.Optimization.Constrainers.Interfaces;

public interface IConstrainer
{
    public double GetPenaltyValue(double[] x);
    public double GetPenaltyValue(Span<double> x);
    public double GetPenaltyValue(ref BurningParams burningParams);
}
