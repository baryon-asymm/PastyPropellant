using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Test.Share.Helpers;

public static class CombustionSolverParamsHelper
{
    public static readonly double[] DefaultVector =
    [
        1.0953581629649699E+308, 4089805.326986677, 39348106041.73285,
        199136.43211252883, 14514342.274935542, 50000.063738034034,
        1311867835.413114, 105642.08691187386, 1.3856, 25564624908.404716,
        329530.9272203463, 622053.027122523, 0.10394228947409136
    ];

    public static CombustionSolverParams GetDefaultCombustionSolverParams() =>
        CombustionSolverParams.FromVector(DefaultVector);

    public static CombustionSolverParamsByDoubles GetDefaultCombustionSolverParamsByDoubles() =>
        CombustionSolverParamsByDoubles.FromVector(DefaultVector);
}
