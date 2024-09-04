namespace ParametricCombustionModel.ReportMaking.Interfaces;

public interface ITransformable<out T>
{
    public T Transform();
}
