#pragma once
#include "../initial_context.hpp"
#include "../solvers/gibbs_energy_solver.hpp"

#include "../../../PastyPropellant.Core.Computatuion/models/constraint.hpp"
#include "../../../PastyPropellant.Core.Computatuion/providers/differential_evolution_provider.hpp"

#pragma managed(push, off)

class 

#pragma managed

inline double func(const std::vector<double>& args)
{
    return get_gibbs_energy(args,
                            initial_context::get_instance().chamber_pressure,
                            initial_context::get_instance().temperature);
}

public ref class CombustionProductsFinder
{
private:
    size_t pop_size_;
    size_t proc_count_;
    bool is_minimize_;
    std::pair<double, double> searching_temperature_range_;

    size_t liquid_substances_offset;
    std::vector<std::vector<double>> substances_coefficients;
    std::vector<std::vector<double>> substances_molar_masses;
    std::vector<std::pair<double, double>> range_temperature_substances;

    void UpdateInitialContext(double temperature)
    {
        initial_context::get_instance().temperature = temperature;
        initial_context::get_instance().gas_substances = std::make_shared<substances_params>();
        initial_context::get_instance().liquid_substances = std::make_shared<substances_params>();
        
        for (size_t i = 0; i < substances_coefficients.size(); ++i)
        {
            if (range_temperature_substances[i].first <= temperature && temperature <= range_temperature_substances[i].second)
            {
                if (i >= liquid_substances_offset)
                {
                    initial_context::get_instance().liquid_substances->add_substance_coefficients(substances_coefficients[i]);
                    initial_context::get_instance().liquid_substances->add_chemical_elements_molar_masses(substances_molar_masses[i]);
                }
                else
                {
                    initial_context::get_instance().gas_substances->add_substance_coefficients(substances_coefficients[i]);
                    initial_context::get_instance().gas_substances->add_chemical_elements_molar_masses(substances_molar_masses[i]);
                }
            }
        }
    }

    double BinarySearchTemperature(double err, const std::function<double(double)>& func)
    {
        double left = searching_temperature_range_.first;
        double right = searching_temperature_range_.second;
        double middle = (left + right) / 2;
        double left_value = func(left);
        double right_value = func(right);
        double middle_value = func(middle);
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
            middle_value = func(middle);
        }

        return middle;
    }

    double GetGibbsEnergy(double temperature)
    {
        UpdateInitialContext(temperature);

        const size_t substances_count = initial_context::get_instance().gas_substances->get_substances_count() +
                                        initial_context::get_instance().liquid_substances->get_substances_count();
        std::vector<std::unique_ptr<constraint>> constraints;
        for (size_t i = 0; i < substances_count; ++i)
            constraints.push_back(std::make_unique<constraint>(1e6, 0.0));

        const differential_evolution_provider differential_evolution_provider(func,
                                                                              pop_size_,
                                                                              proc_count_,
                                                                              std::move(constraints),
                                                                              is_minimize_);
        
        const std::vector<double> result = differential_evolution_provider.run();
        for (size_t i = 0; i < result.size(); ++i)
            std::cout << "x" << i << ": " << result.at(i) << " ";

        return func(result);
    }
    
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
        pop_size_ = popSize;
        proc_count_ = procCount;
        is_minimize_ = isMinimize;

        initial_context::get_instance().chamber_pressure = chamberPressure;
        searching_temperature_range_ = std::make_pair(startSearchTemperature, endSearchTemperature);

        liquid_substances_offset = liquidSubstancesOffset;
        
        substances_coefficients = std::vector<std::vector<double>>();
        for (size_t i = 0; i < substancesCoefficients->Length; ++i)
        {
            substances_coefficients.push_back(std::vector<double>());
            for (size_t j = 0; j < substancesCoefficients[i]->Length; ++j)
                substances_coefficients[i].push_back(static_cast<double>(substancesCoefficients[i][j]));
        }

        substances_molar_masses = std::vector<std::vector<double>>();
        for (size_t i = 0; i < substancesMolarMasses->Length; ++i)
        {
            substances_molar_masses.push_back(std::vector<double>());
            for (size_t j = 0; j < substancesMolarMasses[i]->Length; ++j)
                substances_molar_masses[i].push_back(static_cast<double>(substancesMolarMasses[i][j]));
        }

        range_temperature_substances = std::vector<std::pair<double, double>>();
        for (size_t i = 0; i < minTemperatureSubstances->Length; ++i)
            range_temperature_substances.push_back(
                std::make_pair(
                    static_cast<double>(minTemperatureSubstances[i]),
                    static_cast<double>(maxTemperatureSubstances[i])
                )
            );

        initial_context::get_instance().initial_chemical_elements_molar_masses = std::make_shared<std::vector<double>>();
        for (size_t i = 0; i < initialMolarMasses->Length; ++i)
            initial_context::get_instance().initial_chemical_elements_molar_masses->push_back(static_cast<double>(initialMolarMasses[i]));
    }
    
    System::Void GetCombustionProducts(double err)
    {
        const auto binary_search_func = [this](double temperature)
        {
            return GetGibbsEnergy(temperature) - initial_context::get_instance().initial_enthalpy;
        };

        const double temperature = BinarySearchTemperature(err, binary_search_func);
        std::cout << "Temperature: " << temperature << std::endl;
    }
};
