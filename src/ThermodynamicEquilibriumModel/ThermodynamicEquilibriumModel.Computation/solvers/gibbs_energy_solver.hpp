#pragma once
#include <vector>
#include <iostream>

#include "../initial_context.hpp"

#pragma managed(push, off)

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
        + coefficients[8] * temperature * temperature * temperature * temperature * temperature * temperature *
        temperature
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
            + 7 / 6 * coefficients[8] * temperature * temperature * temperature * temperature * temperature *
            temperature
        )
    );

    if (partial_pressure == 0.0)
        return std_entropy;

    return std_entropy - R * log(partial_pressure / std_pressure);
}

inline std::unique_ptr<std::vector<double>> get_updated_linear_equations_results(
    const std::vector<double>& molar_masses_substances,
    double& total_result)
{
    const auto initial_chemical_molar_masses = initial_context::get_instance().initial_chemical_elements_molar_masses;
    const auto substances = initial_context::get_instance().substances;
    const auto total_substances_count = substances->get_substances_count();
    const auto reserved_substances = initial_context::get_instance().reserved_substances;

    const size_t substances_elements_offset = initial_context::get_instance().substances_elements_offset;
    const size_t offset = total_substances_count - substances_elements_offset;

    auto updated_linear_equations_results = std::make_unique<std::vector<
        double>>(initial_chemical_molar_masses->size());

    for (size_t i = 0; i < initial_chemical_molar_masses->size(); ++i)
    {
        size_t molar_masses_counter = 0;
        double result = initial_chemical_molar_masses->at(i);
        for (size_t j = 0; j < total_substances_count; ++j)
            if (reserved_substances->find(j) == reserved_substances->end())
                result -= (substances->get_chemical_elements_molar_masses(j)[i] * molar_masses_substances[
                    molar_masses_counter++]);
        if (result < 0)
            total_result += result;
        updated_linear_equations_results->at(i) = result;
    }

    return updated_linear_equations_results;
}

inline double get_molar_mass_reserved_substance(const size_t reserved_substance_index,
                                                const std::vector<double>& updated_linear_equations_results)
{
    const auto substances = initial_context::get_instance().substances;
    const auto reserved_substances_elements_pairs = initial_context::get_instance().reserved_substances_elements_pairs;
    return updated_linear_equations_results.at(reserved_substances_elements_pairs->at(reserved_substance_index).second)
        / substances->get_chemical_elements_molar_masses(reserved_substance_index).at(
            reserved_substances_elements_pairs->at(reserved_substance_index).second);
}

inline double get_total_enthalpy(const std::vector<double>& molar_masses_substances,
                                 const double pressure,
                                 const double temperature)
{
    double total_result = 0;
    const auto linear_equations_results = get_updated_linear_equations_results(molar_masses_substances, total_result);
    if (total_result < 0)
        return -total_result;

    const auto substances = initial_context::get_instance().substances;
    const auto reserved_substances = initial_context::get_instance().reserved_substances;
    const auto reserved_elements = initial_context::get_instance().reserved_chemical_elements;
    const auto reserved_substances_elements_pairs = initial_context::get_instance().reserved_substances_elements_pairs;

    const auto gas_substances = initial_context::get_instance().gas_substances;
    const auto liquid_substances = initial_context::get_instance().liquid_substances;

    size_t offset = 0;
    auto updated_molar_masses_substances = std::make_unique<std::vector<double>>();
    for (size_t i = 0; i < substances->get_substances_count(); ++i)
    {
        if (reserved_substances->find(i) != reserved_substances->end())
        {
            updated_molar_masses_substances->push_back(get_molar_mass_reserved_substance(i, *linear_equations_results));
            ++offset;
        }
        else
        {
            updated_molar_masses_substances->push_back(molar_masses_substances[i - offset]);
        }
    }

    const size_t liquid_substances_offset = gas_substances->get_substances_count();

    double total_enthalpy = 0;
    for (size_t i = 0; i < substances->get_substances_count(); ++i)
        total_enthalpy += (
            updated_molar_masses_substances->at(i)
            * get_substance_enthalpy(substances->get_substance_coefficients(i), temperature)
        );

    return total_enthalpy;
}

inline double get_gibbs_energy(const std::vector<double>& molar_masses_substances,
                               const double pressure,
                               const double temperature)
{
    double total_result = 0;
    const auto linear_equations_results = get_updated_linear_equations_results(molar_masses_substances, total_result);
    if (total_result < 0)
        return -total_result;

    const auto substances = initial_context::get_instance().substances;
    const auto reserved_substances = initial_context::get_instance().reserved_substances;
    const auto reserved_elements = initial_context::get_instance().reserved_chemical_elements;
    const auto reserved_substances_elements_pairs = initial_context::get_instance().reserved_substances_elements_pairs;
    /*auto matrix = std::make_shared<std::vector<std::vector<double>>>();
    for (size_t i = 0; i < linear_equations_results->size(); ++i)
    {
        std::vector<double> row;
        size_t molar_masses_counter = 0;
        auto reserved_substances_iterator = reserved_substances->begin();
        while (reserved_substances_iterator != reserved_substances->end())
		{
			row.push_back(substances->get_chemical_elements_molar_masses(*reserved_substances_iterator)[i]);
			++reserved_substances_iterator;
		}
        row.push_back(linear_equations_results->at(i));
        matrix->push_back(row);
    }*/
    //gaussianElimination(matrix);// TODO: нужно сделать set для гарантии существования решения

    const auto gas_substances = initial_context::get_instance().gas_substances;
    const auto liquid_substances = initial_context::get_instance().liquid_substances;

    size_t offset = 0;
    auto updated_molar_masses_substances = std::make_unique<std::vector<double>>();
    for (size_t i = 0; i < substances->get_substances_count(); ++i)
    {
        if (reserved_substances->find(i) != reserved_substances->end())
        {
            updated_molar_masses_substances->push_back(get_molar_mass_reserved_substance(i, *linear_equations_results));
            ++offset;
        }
        else
        {
            updated_molar_masses_substances->push_back(molar_masses_substances[i - offset]);
        }
    }

    const auto initial_chemical_molar_masses = initial_context::get_instance().initial_chemical_elements_molar_masses;
    const auto total_substances_count = substances->get_substances_count();
    auto results = std::make_unique<std::vector<double>>(initial_chemical_molar_masses->size());
    for (size_t i = 0; i < initial_chemical_molar_masses->size(); ++i)
    {
        double result = 0;
        for (size_t j = 0; j < total_substances_count; ++j)
            result += (substances->get_chemical_elements_molar_masses(j)[i] * updated_molar_masses_substances->at(j));
        results->at(i) = result;
    }

    const size_t liquid_substances_offset = gas_substances->get_substances_count();

    double gas_substances_molar_mass = 0;
    for (size_t i = 0; i < gas_substances->get_substances_count(); ++i)
        gas_substances_molar_mass += updated_molar_masses_substances->at(i);

    double gibbs_energy = 0;
    for (size_t i = 0; i < gas_substances->get_substances_count(); ++i)
        gibbs_energy += (updated_molar_masses_substances->at(i) * (
            get_substance_enthalpy(gas_substances->get_substance_coefficients(i), temperature)
            - temperature * get_substance_entropy(gas_substances->get_substance_coefficients(i),
                                                  temperature,
                                                  updated_molar_masses_substances->at(i) * pressure /
                                                  gas_substances_molar_mass)
        ));

    for (size_t i = 0; i < liquid_substances->get_substances_count(); ++i)
        gibbs_energy += (updated_molar_masses_substances->at(i + liquid_substances_offset) * (
            get_substance_enthalpy(liquid_substances->get_substance_coefficients(i), temperature)
            - temperature * get_substance_entropy(liquid_substances->get_substance_coefficients(i),
                                                  temperature)
        ));

    return gibbs_energy;
}
