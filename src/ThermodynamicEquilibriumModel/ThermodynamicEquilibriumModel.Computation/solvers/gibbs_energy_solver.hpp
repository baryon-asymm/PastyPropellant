#pragma once
#include <vector>
#include <iostream>

#include "../initial_context.hpp"

inline double get_substance_enthalpy(const std::vector<double>& coefficients, double temperature)
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
        + coefficients[8] * temperature * temperature * temperature * temperature * temperature * temperature * temperature
    );
}

inline double get_substance_entropy(const std::vector<double>& coefficients,
                                    double temperature,
                                    double partial_pressure = 0.0)
{
    constexpr double R = 8.314;
    constexpr double std_pressure = 101325.0;
    temperature *= 1e-3;
    const double std_entropy = 4.184 * (
        coefficients[0]
        + 1e-3 * coefficients[2] * log(temperature)
        + 1e-3 * (
            2 * coefficients[3] * temperature
            + 3 / 2 * coefficients[4] * temperature * temperature
            + 4 / 3 * coefficients[5] * temperature * temperature * temperature
            + 5 / 4 * coefficients[6] * temperature * temperature * temperature * temperature
            + 6 / 5 * coefficients[7] * temperature * temperature * temperature * temperature * temperature
            + 7 / 6 * coefficients[8] * temperature * temperature * temperature * temperature * temperature * temperature
        )
    );

    if (partial_pressure == 0.0)
        return std_entropy;
    
    return std_entropy - R * log(partial_pressure / std_pressure);
}

inline double get_gibbs_energy(const std::vector<double>& molar_masses_substances,
                               const double pressure,
                               const double temperature)
{
    const auto gas_substances = initial_context::get_instance().gas_substances;
    const auto liquid_substances = initial_context::get_instance().liquid_substances;

    const size_t liquid_substances_offset = gas_substances->get_substances_count();

    double gas_substances_molar_mass = 0;
    for (size_t i = 0; i < gas_substances->get_substances_count(); ++i)
        gas_substances_molar_mass += molar_masses_substances[i];

    double gibbs_energy = 0;
    for (size_t i = 0; i < gas_substances->get_substances_count(); ++i)
        gibbs_energy += molar_masses_substances[i] * (
            get_substance_enthalpy(gas_substances->get_substance_coefficients(i), temperature)
            - temperature * get_substance_entropy(gas_substances->get_substance_coefficients(i),
                                                  temperature,
                                                  molar_masses_substances[i] * pressure / gas_substances_molar_mass)
        );

    for (size_t i = 0; i < liquid_substances->get_substances_count(); ++i)
        gibbs_energy += molar_masses_substances[i + liquid_substances_offset] * (
            get_substance_enthalpy(liquid_substances->get_substance_coefficients(i), temperature)
            - temperature * get_substance_entropy(liquid_substances->get_substance_coefficients(i), temperature)
        );

    return gibbs_energy;
}
