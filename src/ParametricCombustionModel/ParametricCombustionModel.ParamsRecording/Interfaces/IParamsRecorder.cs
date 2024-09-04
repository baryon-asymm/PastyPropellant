namespace ParametricCombustionModel.ParamsRecording.Interfaces;

public interface IParamsRecorder<out T>
{
    public T GetRecord(Span<double> surfaceTemperatures, ref BurningParams burningParams);
}
