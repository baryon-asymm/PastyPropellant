#pragma once
#include <memory>
#include <unordered_set>

#include "utils/substances_params.hpp"

#pragma managed(push, off)

class initial_context
{
    initial_context()
    {
    }

    initial_context(const initial_context&);
    void operator=(const initial_context&);

public:
    static initial_context& get_instance()
    {
        static initial_context instance;
        return instance;
    }

    std::shared_ptr<substances_params> gas_substances;
    std::shared_ptr<substances_params> liquid_substances;

    std::shared_ptr<substances_params> substances;

    std::shared_ptr<std::vector<double>> initial_chemical_elements_molar_masses;
    std::shared_ptr<std::unordered_set<size_t>> reserved_substances;
    std::shared_ptr<std::unordered_set<size_t>> reserved_chemical_elements;

    std::shared_ptr<std::vector<std::pair<size_t, int>>> reserved_substances_elements_pairs;

    size_t substances_elements_offset;

    double temperature;
    double chamber_pressure;

    double initial_enthalpy;
};
