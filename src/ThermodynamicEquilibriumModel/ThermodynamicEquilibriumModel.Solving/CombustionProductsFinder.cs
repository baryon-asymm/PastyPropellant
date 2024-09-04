using ThermodynamicHelper = ThermodynamicEquilibriumModel.Solving.Helpers.SubstanceThermodynamicStateHelper;

namespace ThermodynamicEquilibriumModel.Solving;

public class CombustionProductsFinder
{
    private double _temperature;

    public CombustionProductsFinder(double[] initialElementsCounts,
                                    double[][] substancesCoefficients,
                                    double[][] substancesElementsCounts,
                                    int liquidSubstancesOffset)
    {
        LiquidSubstancesOffset = liquidSubstancesOffset;
        SubstancesCoefficients = substancesCoefficients;
        SubstancesElementsCounts = substancesElementsCounts;
        InitialElementsCounts = initialElementsCounts;
    }

    public int LiquidSubstancesOffset { get; }

    public double[][] SubstancesCoefficients { get; }
    public double[][] SubstancesElementsCounts { get; }
    public double[] InitialElementsCounts { get; }

    private void SolvingFunc(double[] x, double[] fi, object obj)
    {
        var checkElementsSum = new double[InitialElementsCounts.Length];
        for (var i = 0; i < SubstancesCoefficients.Length; i++)
        for (var j = 0; j < InitialElementsCounts.Length; j++)
            checkElementsSum[j] += SubstancesElementsCounts[i][j] * x[i];

        var gasSubstancesMolarMasses = GetTotalGasSubstancesMolarMasses(x);

        double gibbsEnergy = 0;
        for (var i = 0; i < LiquidSubstancesOffset; i++)
            gibbsEnergy += x[i] * ThermodynamicHelper.GetGibbsEnergy(SubstancesCoefficients[i],
                                                                     _temperature,
                                                                     x[i] / gasSubstancesMolarMasses * 100 * 101325.0);
        for (var i = LiquidSubstancesOffset; i < SubstancesCoefficients.Length; i++)
            gibbsEnergy += x[i] * ThermodynamicHelper.GetGibbsEnergy(SubstancesCoefficients[i],
                                                                     _temperature);

        fi[0] = gibbsEnergy;
    }

    private double GetTotalGasSubstancesMolarMasses(Span<double> molarMasses)
    {
        double totalMolarMass = 0;
        for (var i = 0; i < LiquidSubstancesOffset; i++)
            totalMolarMass += molarMasses[i];
        return totalMolarMass;
    }

    private double GetEnthalpyForTemperature(double temperature, out double[] elementsMolars)
    {
        try
        {
            var initialPoint = new double[SubstancesCoefficients.Length];
            for (var i = 0; i < initialPoint.Length; i++)
                initialPoint[i] = 1;

            _temperature = temperature;

            var scales = new double[SubstancesCoefficients.Length];
            for (var i = 0; i < scales.Length; i++)
                scales[i] = 1;

            var conditionsTypes = new int[InitialElementsCounts.Length];
            for (var i = 0; i < conditionsTypes.Length; i++)
                conditionsTypes[i] = 0;

            var constraints = new double[InitialElementsCounts.Length, SubstancesElementsCounts.Length + 1];
            for (var i = 0; i < InitialElementsCounts.Length; i++)
            {
                for (var j = 0; j < SubstancesElementsCounts.Length; j++)
                    constraints[i, j] = SubstancesElementsCounts[j][i];
                constraints[i, SubstancesElementsCounts.Length] = InitialElementsCounts[i];
            }

            var ub = new double[SubstancesCoefficients.Length];
            for (var i = 0; i < ub.Length; i++)
                ub[i] = double.MaxValue;

            var lb = new double[SubstancesCoefficients.Length];
            for (var i = 0; i < lb.Length; i++)
                lb[i] = 0;

            //double[] x = new double[] { 5, 5 };
            //double[] s = new double[] { 1, 1 };
            //double[,] c = new double[,] { { 1, 0, 2 }, { 1, 1, 6 } };
            //int[] ct = new int[] { 1, 1 };
            alglib.minnlcstate state;
            double epsg = 0;
            double epsf = 0;
            double epsx = 0;
            var maxits = 0;

            alglib.minnlccreatef(initialPoint, 1e-6, out state);
            alglib.minnlcsetbc(state, lb, ub);
            alglib.minnlcsetlc(state, constraints, conditionsTypes);
            //alglib.minbleicsetscale(state, scales);
            //alglib.minnlcsetcond(state, epsx, maxits);

            //alglib.minbleicoptguardsmoothness(state);
            //alglib.minbleicoptguardgradient(state, 0.001);

            alglib.minnlcreport rep;
            alglib.minnlcoptimize(state, SolvingFunc, null, null);
            alglib.minnlcresults(state, out initialPoint, out rep);
            Console.WriteLine("{0}", rep.terminationtype);
            Console.WriteLine("{0}", alglib.ap.format(initialPoint, 2));
            var fi = new double[1];
            SolvingFunc(initialPoint, fi, null);
            Console.WriteLine($"gibbs {fi[0]}");

            elementsMolars = initialPoint;

            double enthalpy = 0;
            for (var i = 0; i < LiquidSubstancesOffset; i++)
                enthalpy += initialPoint[i] * ThermodynamicHelper.GetEnthalpy(SubstancesCoefficients[i], temperature);

            for (var i = LiquidSubstancesOffset; i < SubstancesCoefficients.Length; i++)
                enthalpy += initialPoint[i] * ThermodynamicHelper.GetEnthalpy(SubstancesCoefficients[i], temperature);

            Console.WriteLine($"enthalpy {enthalpy} for temp: {_temperature}");

            var checkElementsSum = new double[InitialElementsCounts.Length];
            for (var i = 0; i < SubstancesCoefficients.Length; i++)
            for (var j = 0; j < InitialElementsCounts.Length; j++)
                checkElementsSum[j] += SubstancesElementsCounts[i][j] * initialPoint[i];

            return enthalpy + 1199461.0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        elementsMolars = new double[0];

        return 0;
    }

    public void Find()
    {
        double[] elementsMolars;

        double left = 1000;
        double right = 5000;
        var middle = (left + right) / 2;
        var left_value = GetEnthalpyForTemperature(left, out elementsMolars);
        var right_value = GetEnthalpyForTemperature(right, out elementsMolars);
        var middle_value = GetEnthalpyForTemperature(middle, out elementsMolars);
        while (right - left > 0.1)
        {
            if (middle_value < 0)
            {
                left = middle;
                left_value = middle_value;
            }
            else
            {
                right = middle;
                right_value = middle_value;
            }

            middle = (left + right) / 2;
            middle_value = GetEnthalpyForTemperature(middle, out elementsMolars);
        }

        Console.WriteLine($"Temperature: {middle}");
    }
}
