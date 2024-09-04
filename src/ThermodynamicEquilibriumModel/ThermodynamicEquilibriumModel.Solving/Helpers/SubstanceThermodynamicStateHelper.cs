namespace ThermodynamicEquilibriumModel.Solving.Helpers;

public static class SubstanceThermodynamicStateHelper
{
    public static double GetEnthalpy(Span<double> coefficients, double temperature)
    {
        temperature *= 1e-3;
        return 4.184 * (
            coefficients[1]
            + coefficients[2] * temperature
            + coefficients[3] * temperature * temperature
            + coefficients[4] * temperature * temperature * temperature
            + coefficients[5] * temperature * temperature * temperature * temperature
            + coefficients[6] * temperature * temperature * temperature * temperature * temperature
            + coefficients[7] * temperature * temperature * temperature * temperature * temperature * temperature
            + coefficients[8] * temperature * temperature * temperature * temperature * temperature * temperature *
            temperature
        );
    }

    public static double GetEntropy(Span<double> coefficients, double temperature)
    {
        return GetStandartEntropy(coefficients, temperature);
    }

    public static double GetEntropy(Span<double> coefficients, double temperature, double partialPressure)
    {
        const double R = 8.314;
        const double STANDART_PRESSURE = 101325.0;

        var standartEntropy = GetStandartEntropy(coefficients, temperature);

        return standartEntropy - R * Math.Log(partialPressure / STANDART_PRESSURE);
    }

    public static double GetGibbsEnergy(Span<double> coefficients, double temperature)
    {
        return GetEnthalpy(coefficients, temperature) - temperature * GetEntropy(coefficients, temperature);
    }

    public static double GetGibbsEnergy(Span<double> coefficients, double temperature, double partialPressure)
    {
        if (partialPressure == 0)
            return GetGibbsEnergy(coefficients, temperature);

        return GetEnthalpy(coefficients, temperature) -
               temperature * GetEntropy(coefficients, temperature, partialPressure);
    }

    private static double GetStandartEntropy(Span<double> coefficients, double temperature)
    {
        temperature *= 1e-3;
        return 4.184 * (
            coefficients[0]
            + 1e-3 * coefficients[2] * Math.Log(temperature)
            + 1e-3 * (
                2 * coefficients[3] * temperature
                + 3 / 2 * coefficients[4] * temperature * temperature
                + 4 / 3 * coefficients[5] * temperature * temperature * temperature
                + 5 / 4 * coefficients[6] * temperature * temperature * temperature * temperature
                + 6 / 5 * coefficients[7] * temperature * temperature * temperature * temperature * temperature
                + 7 / 6 * coefficients[8] * temperature * temperature * temperature * temperature * temperature *
                temperature
            )
        );
    }
}
