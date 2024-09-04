using System.Collections.ObjectModel;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.ParamsRecording.Builders;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;
using ParametricCombustionModel.Test.Common.Models;

namespace ParametricCombustionModel.Optimization.Test.Models;

public class DefaultMatrixesHolder
{
    public static ReadOnlyCollection<ReadOnlyCollection<MixedSolverParamsRecorder>> SolversMatrix
    {
        get
        {
            Propellant[] propellants =
                [DefaultPropellant.Propellant, DefaultPropellant.Propellant, DefaultPropellant.Propellant];
            return MixedSolverParamsRecordersBuilder.FromPropellants(propellants)
                                                    .ForPressures(DefaultPressuresHolder.Pressures)
                                                    .Build();
        }
    }

    public static ReadOnlyCollection<ReadOnlyCollection<double>> ExperimentalBurningRatesMatrix
    {
        get
        {
            Propellant[] propellants =
                [DefaultPropellant.Propellant, DefaultPropellant.Propellant, DefaultPropellant.Propellant];
            return propellants.GetExperimentalBurningRates(DefaultPressuresHolder.Pressures);
        }
    }
}
