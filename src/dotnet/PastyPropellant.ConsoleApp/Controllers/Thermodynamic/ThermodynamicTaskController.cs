using PastyPropellant.Core.Models.Thermodynamic;

namespace PastyPropellant.ConsoleApp.Controllers.Thermodynamic;

public class ThermodynamicTaskController
{
    private readonly List<ThermodynamicTask> _tasks;

    public ThermodynamicTaskController(List<ThermodynamicTask> tasks)
    {
        _tasks = tasks;
    }

    public void Run()
    {
        foreach (var task in _tasks)
        {
            var handler = task.CombustionProductsHandler;
            var elements = handler.GetChemicalElements();
            var substances = handler.GetPossibleCombustionSubstances();
            var molars = handler.GetCombustionProductsElementsMolars();
            var initialMolars = handler.GetPropellantChemicalElementsMolars();

            var initialMolarMasses = new double[elements.Length];
            for (var i = 0; i < initialMolars.Length; i++)
                initialMolarMasses[i] = initialMolars[i];

            var substancesCoefficients = new double[substances.Length][];
            for (var i = 0; i < substances.Length; i++)
            {
                substancesCoefficients[i] = new double[substances[i].Coefficients.Count];
                for (var j = 0; j < substances[i].Coefficients.Count; j++)
                    substancesCoefficients[i][j] = substances[i].Coefficients[j];
            }

            var substancesMolarMasses = new double[substances.Length][];
            for (var i = 0; i < substances.Length; i++)
            {
                substancesMolarMasses[i] = new double[elements.Length];
                for (var j = 0; j < elements.Length; j++)
                    substancesMolarMasses[i][j] = molars[i][j];
            }

            var liquidOffset = 0;
            for (var i = 0; i < substances.Length; i++)
                if (substances[i].Phase == SubstancePhase.Gas)
                    liquidOffset++;

            //var finder = new ThermodynamicEquilibriumModel.Solving.CombustionProductsFinder(initialMolarMasses, substancesCoefficients, substancesMolarMasses, liquidOffset);
            //finder.Find();

            /*var coefficients = new double[substances.Length][];
            var molarMasses = new double[substances.Length][];
            var minTemperatures = new double[substances.Length];
            var maxTemperatures = new double[substances.Length];
            var initialMolarMasses = new double[elements.Length];

            ulong liquidOffset = 0;

            for (int i = 0; i < substances.Length; i++)
            {
                coefficients[i] = new double[substances[i].Coefficients.Count];
                molarMasses[i] = new double[elements.Length];
                for (int j = 0; j < substances[i].Coefficients.Count; j++)
                    coefficients[i][j] = substances[i].Coefficients[j];

                for (int j = 0; j < elements.Length; j++)
                    molarMasses[i][j] = molars[i][j];

                minTemperatures[i] = substances[i].TemperatureRange.Min;
                maxTemperatures[i] = substances[i].TemperatureRange.Max;

                if (substances[i].Phase == SubstancePhase.Gas)
                    liquidOffset++;
            }

            for (int i = 0; i < initialMolars.Length; i++)
                initialMolarMasses[i] = initialMolars[i];

            //var test = new test_solver();
            //test.test();
            //return;

            var finder = new CombustionProductsFinder(
                1000,
                12,
                true,
                100 * 101325.0,
                1000,
                4999,
                liquidOffset,
                coefficients,
                molarMasses,
                minTemperatures,
                maxTemperatures,
                initialMolarMasses
            );

            finder.GetCombustionProducts(1e-1);*/
        }
    }
}
