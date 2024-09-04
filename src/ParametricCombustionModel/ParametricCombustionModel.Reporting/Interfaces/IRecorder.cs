using ParametricCombustionModel.Computation.Models.ComputationsParams;

namespace ParametricCombustionModel.Reporting.Interfaces;

public interface IRecorder<T>
{
    public T GetRecord(Span<double> surfaceTemperatures, ref BurningParams burningParams);
}
