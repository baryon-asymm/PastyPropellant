using System.Numerics;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Units;

public enum AMetalBurningConstantUnit
{
    SquareMetersPerSecond
}

public readonly struct AMetalBurningConstant
    : IDivisionOperators<AMetalBurningConstant, Speed, Length>
{
#region Fields

    private readonly double _value;

    private readonly AMetalBurningConstantUnit? _unit;

#endregion

#region Constructors

    public AMetalBurningConstant(
        double value,
        AMetalBurningConstantUnit unit)
    {
        _value = value;
        _unit = unit;
    }

#endregion

#region Properties

    public double SquareMetersPerSecond =>
        _unit switch
        {
            AMetalBurningConstantUnit.SquareMetersPerSecond => _value,
            _ => throw new NotImplementedException()
        };

#endregion

#region Operators

    public static Length operator /(
        AMetalBurningConstant left,
        Speed right)
    {
        return Length.FromMeters(
            left.SquareMetersPerSecond / right.MetersPerSecond);
    }

#endregion

#region Methods

    public static AMetalBurningConstant FromSquareMetersPerSecond(
        double value)
    {
        return new AMetalBurningConstant(value, AMetalBurningConstantUnit.SquareMetersPerSecond);
    }

    public override string ToString()
    {
        return $"{_value:0.00e+00} m²/s";
    }

#endregion
}
