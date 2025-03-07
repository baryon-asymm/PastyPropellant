using System.Numerics;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Units;

public enum BMetalBurningConstantUnit
{
    CubicMetersPerSquareSecond
}

public readonly struct BMetalBurningConstant
    : IDivisionOperators<BMetalBurningConstant, Speed, AMetalBurningConstant>
{
#region Fields

    private readonly double _value;

    private readonly BMetalBurningConstantUnit? _unit;

#endregion

#region Constructors

    public BMetalBurningConstant(
        double value,
        BMetalBurningConstantUnit unit)
    {
        _value = value;
        _unit = unit;
    }

#endregion

#region Properties

    public double CubicMetersPerSquareSecond =>
        _unit switch
        {
            BMetalBurningConstantUnit.CubicMetersPerSquareSecond => _value,
            _ => throw new NotImplementedException()
        };

#endregion

#region Operators

    public static AMetalBurningConstant operator /(
        BMetalBurningConstant left,
        Speed right)
    {
        return new AMetalBurningConstant(
            left.CubicMetersPerSquareSecond / right.MetersPerSecond,
            AMetalBurningConstantUnit.SquareMetersPerSecond);
    }

    #endregion

    #region Methods

    public static BMetalBurningConstant FromCubicMetersPerSquareSecond(
        double value)
    {
        return new BMetalBurningConstant(value, BMetalBurningConstantUnit.CubicMetersPerSquareSecond);
    }

    public override string ToString()
    {
        return $"{_value:0.00e+00} m³/s²";
    }

#endregion
}
