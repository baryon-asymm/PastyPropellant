#pragma once
#include <unordered_set>

#include "../initial_context.hpp"
#include "../solvers/gibbs_energy_solver.hpp"

#include "../../../PastyPropellant.Core.Computatuion/models/constraint.hpp"
#include "../../../PastyPropellant.Core.Computatuion/providers/differential_evolution_provider.hpp"

#pragma managed

inline double func(const std::vector<double>& args)
{
    return get_gibbs_energy(args,
                            initial_context::get_instance().chamber_pressure,
                            initial_context::get_instance().temperature);
}

class combustion_products_finder
{
    size_t pop_size_;
    size_t proc_count_;
    bool is_minimize_;
    std::pair<double, double> searching_temperature_range_;

    size_t liquid_substances_offset_;
    std::vector<std::vector<double>> substances_coefficients_;
    std::vector<std::vector<double>> substances_molar_masses_;
    std::vector<std::pair<double, double>> range_temperature_substances_;

    void update_initial_context(const double temperature) const
    {
        initial_context::get_instance().temperature = temperature;
        initial_context::get_instance().gas_substances = std::make_shared<substances_params>();
        initial_context::get_instance().liquid_substances = std::make_shared<substances_params>();
        initial_context::get_instance().substances = std::make_shared<substances_params>();

        for (size_t i = 0; i < substances_coefficients_.size(); ++i)
        {
            if (range_temperature_substances_[i].first <= temperature && temperature < range_temperature_substances_[i].
                second)
            {
                if (i >= liquid_substances_offset_)
                {
                    initial_context::get_instance().liquid_substances->add_substance_coefficients(
                        substances_coefficients_[i]);
                    initial_context::get_instance().liquid_substances->add_chemical_elements_molar_masses(
                        substances_molar_masses_[i]);
                }
                else
                {
                    initial_context::get_instance().gas_substances->add_substance_coefficients(
                        substances_coefficients_[i]);
                    initial_context::get_instance().gas_substances->add_chemical_elements_molar_masses(
                        substances_molar_masses_[i]);
                }

                initial_context::get_instance().substances->add_substance_coefficients(substances_coefficients_[i]);
                initial_context::get_instance().substances->add_chemical_elements_molar_masses(
                    substances_molar_masses_[i]);
            }
        }

        const auto substances = initial_context::get_instance().substances;
        const auto reserved_substances_elements_pairs = std::make_shared<std::vector<std::pair<size_t, int>>>();
        for (size_t i = 0; i < substances->get_substances_count(); ++i)
            reserved_substances_elements_pairs->push_back(std::make_pair(i, -1));
        auto reserved_substances = std::make_shared<std::unordered_set<size_t>>();
        auto reserved_elements = std::make_shared<std::unordered_set<size_t>>();
        for (size_t i = 0; i < substances->get_substances_count(); ++i)
        {
            bool is_reserved = true;
            int element_molars = -1;
            size_t reserved_substances_number = 0;
            size_t reserved_elements_number = 0;
            for (size_t j = 0; j < initial_context::get_instance().initial_chemical_elements_molar_masses->size(); ++j)
            {
                if (element_molars == -1 && substances->get_chemical_elements_molar_masses(i)[j] != 0)
                {
                    element_molars = substances->get_chemical_elements_molar_masses(i)[j];
                    reserved_substances_number = i;
                    reserved_elements_number = j;
                }
                else if (element_molars != -1 && substances->get_chemical_elements_molar_masses(i)[j] != 0)
                {
                    is_reserved = false;
                    break;
                }
            }

            if (is_reserved && reserved_elements->find(reserved_elements_number) == reserved_elements->end())
            {
                reserved_substances->insert(reserved_substances_number);
                reserved_elements->insert(reserved_elements_number);
                reserved_substances_elements_pairs->at(reserved_substances_number).second = reserved_elements_number;
            }
        }

        initial_context::get_instance().reserved_substances_elements_pairs = reserved_substances_elements_pairs;

        initial_context::get_instance().reserved_substances = reserved_substances;
        initial_context::get_instance().reserved_chemical_elements = reserved_elements;

        const size_t substances_count = initial_context::get_instance().gas_substances->get_substances_count()
            + initial_context::get_instance().liquid_substances->get_substances_count();
        const size_t chemical_elements_count = initial_context::get_instance().initial_chemical_elements_molar_masses->
                                                                               size();
        const size_t diffs_vars_count = substances_count - chemical_elements_count;
        initial_context::get_instance().substances_elements_offset = diffs_vars_count;
    }

    double binary_search_temperature(const double err) const
    {
        double left = searching_temperature_range_.first;
        double right = searching_temperature_range_.second;
        double middle = (left + right) / 2;
        double left_value = calc_gibbs_energy_error(left);
        double right_value = calc_gibbs_energy_error(right);
        double middle_value = calc_gibbs_energy_error(middle);
        while (right - left > err)
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
            middle_value = calc_gibbs_energy_error(middle);
        }

        return middle;
    }

    double calc_gibbs_energy(const double temperature) const
    {
        update_initial_context(temperature);

        auto substances = initial_context::get_instance().substances;
        auto reserved_substances = initial_context::get_instance().reserved_substances;
        auto initial_chemical_elements = initial_context::get_instance().initial_chemical_elements_molar_masses;

        std::vector<std::unique_ptr<constraint>> constraints;
        for (size_t i = 0; i < substances->get_substances_count(); ++i)
            if (reserved_substances->find(i) == reserved_substances->end())
            {
                double max_molar_mass = 1e6;
                for (size_t j = 0; j < initial_chemical_elements->size(); ++j)
                    if (substances->get_chemical_elements_molar_masses(i)[j] != 0)
                        max_molar_mass = std::min(max_molar_mass,
                                                  initial_chemical_elements->at(j) / substances->
                                                  get_chemical_elements_molar_masses(i)[j]);
                constraints.push_back(std::make_unique<constraint>(max_molar_mass, 0));
            }

        const differential_evolution_provider differential_evolution_provider(func,
                                                                              pop_size_,
                                                                              proc_count_,
                                                                              std::move(constraints),
                                                                              is_minimize_);
        std::cout << "Running differential_evolution_provider" << std::endl;

        const std::vector<double> result = differential_evolution_provider.run();
        for (size_t i = 0; i < result.size(); ++i)
            std::cout << "x" << i << ": " << result.at(i) << " ";
        double total_enthalpy = get_total_enthalpy(result,
                                                   initial_context::get_instance().chamber_pressure,
                                                   initial_context::get_instance().temperature);
        std::cout << std::endl << "Cost: " << total_enthalpy - initial_context::get_instance().initial_enthalpy <<
            std::endl;

        return total_enthalpy;
    }

    double calc_gibbs_energy_error(const double temperature) const
    {
        std::cout << "Temperature: " << temperature << std::endl;
        return calc_gibbs_energy(temperature) - initial_context::get_instance().initial_enthalpy;
        // TODO: change to initial_gibbs_energy
    }

public:
    combustion_products_finder(const size_t pop_size,
                               const size_t proc_count,
                               const bool is_minimize,
                               const double chamber_pressure,
                               double start_search_temperature,
                               double end_search_temperature,
                               const size_t liquid_substances_offset,
                               const std::vector<std::vector<double>>& substances_coefficients,
                               const std::vector<std::vector<double>>& substances_molar_masses,
                               const std::vector<double>& min_temperature_substances,
                               const std::vector<double>& max_temperature_substances,
                               const std::vector<double>& initial_molar_masses)
    {
        pop_size_ = pop_size;
        proc_count_ = proc_count;
        is_minimize_ = is_minimize;

        initial_context::get_instance().chamber_pressure = chamber_pressure;
        searching_temperature_range_ = std::make_pair(start_search_temperature, end_search_temperature);

        liquid_substances_offset_ = liquid_substances_offset;

        substances_coefficients_ = std::vector<std::vector<double>>();
        for (size_t i = 0; i < substances_coefficients.size(); ++i)
        {
            substances_coefficients_.push_back(std::vector<double>());
            for (size_t j = 0; j < substances_coefficients[i].size(); ++j)
                substances_coefficients_[i].push_back(static_cast<double>(substances_coefficients[i][j]));
        }

        substances_molar_masses_ = std::vector<std::vector<double>>();
        for (size_t i = 0; i < substances_molar_masses.size(); ++i)
        {
            substances_molar_masses_.push_back(std::vector<double>());
            for (size_t j = 0; j < substances_molar_masses[i].size(); ++j)
                substances_molar_masses_[i].push_back(static_cast<double>(substances_molar_masses[i][j]));
        }

        range_temperature_substances_ = std::vector<std::pair<double, double>>();
        for (size_t i = 0; i < min_temperature_substances.size(); ++i)
            range_temperature_substances_.push_back(
                std::make_pair(
                    static_cast<double>(min_temperature_substances[i]),
                    static_cast<double>(max_temperature_substances[i])
                )
            );

        initial_context::get_instance().initial_chemical_elements_molar_masses = std::make_shared<std::vector<
            double>>();
        for (size_t i = 0; i < initial_molar_masses.size(); ++i)
            initial_context::get_instance().initial_chemical_elements_molar_masses->push_back(
                static_cast<double>(initial_molar_masses[i]));

        initial_context::get_instance().initial_enthalpy = -1199461.0;
    }

    void get_combustion_products(const double err) const
    {
        const double temperature = binary_search_temperature(err);
        std::cout << "Temperature: " << temperature << std::endl;
    }
};

public ref class CombustionProductsFinder
{
    combustion_products_finder* combustion_products_finder_;

public:
    CombustionProductsFinder(size_t popSize,
                             size_t procCount,
                             bool isMinimize,
                             double chamberPressure,
                             double startSearchTemperature,
                             double endSearchTemperature,
                             size_t liquidSubstancesOffset,
                             array<array<double>^>^ substancesCoefficients,
                             array<array<double>^>^ substancesMolarMasses,
                             array<double>^ minTemperatureSubstances,
                             array<double>^ maxTemperatureSubstances,
                             array<double>^ initialMolarMasses)
    {
        using System::IntPtr;
        using System::Runtime::InteropServices::Marshal;

        std::vector<std::vector<double>> substancesCoefficientsVector(substancesCoefficients->Length);
        std::vector<std::vector<double>> substancesMolarMassesVector(substancesMolarMasses->Length);
        std::vector<double> minTemperatureSubstancesVector(minTemperatureSubstances->Length);
        std::vector<double> maxTemperatureSubstancesVector(maxTemperatureSubstances->Length);
        std::vector<double> initialMolarMassesVector(initialMolarMasses->Length);

        for (size_t i = 0; i < substancesCoefficients->Length; ++i)
        {
            substancesCoefficientsVector[i] = std::vector<double>(substancesCoefficients[i]->Length);
            Marshal::Copy(substancesCoefficients[i], 0, IntPtr(&substancesCoefficientsVector[i][0]),
                          substancesCoefficients[i]->Length);
        }

        for (size_t i = 0; i < substancesMolarMasses->Length; ++i)
        {
            substancesMolarMassesVector[i] = std::vector<double>(substancesMolarMasses[i]->Length);
            Marshal::Copy(substancesMolarMasses[i], 0, IntPtr(&substancesMolarMassesVector[i][0]),
                          substancesMolarMasses[i]->Length);
        }

        Marshal::Copy(minTemperatureSubstances, 0, IntPtr(&minTemperatureSubstancesVector[0]),
                      minTemperatureSubstances->Length);
        Marshal::Copy(maxTemperatureSubstances, 0, IntPtr(&maxTemperatureSubstancesVector[0]),
                      maxTemperatureSubstances->Length);
        Marshal::Copy(initialMolarMasses, 0, IntPtr(&initialMolarMassesVector[0]), initialMolarMasses->Length);

        combustion_products_finder_ = new combustion_products_finder(popSize,
                                                                     procCount,
                                                                     isMinimize,
                                                                     chamberPressure,
                                                                     startSearchTemperature,
                                                                     endSearchTemperature,
                                                                     liquidSubstancesOffset,
                                                                     std::move(substancesCoefficientsVector),
                                                                     std::move(substancesMolarMassesVector),
                                                                     std::move(minTemperatureSubstancesVector),
                                                                     std::move(maxTemperatureSubstancesVector),
                                                                     std::move(initialMolarMassesVector));
    }

    System::Void GetCombustionProducts(double err)
    {
        combustion_products_finder_->get_combustion_products(err);
    }
};
