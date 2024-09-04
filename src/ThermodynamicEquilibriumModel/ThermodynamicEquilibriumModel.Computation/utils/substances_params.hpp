#pragma once
#include <vector>

#pragma managed(push, off)

class substances_params
{
    std::vector<std::vector<double>> chemical_elements_molar_masses_;
    std::vector<std::vector<double>> coefficients_approximating_polynomial_;

public:
    void add_chemical_elements_molar_masses(const std::vector<double>& molar_masses)
    {
        chemical_elements_molar_masses_.push_back(molar_masses);
    }

    void add_substance_coefficients(const std::vector<double>& coefficients)
    {
        coefficients_approximating_polynomial_.push_back(coefficients);
    }

    const std::vector<double>& get_chemical_elements_molar_masses(const size_t index) const
    {
        return chemical_elements_molar_masses_[index];
    }

    const std::vector<double>& get_substance_coefficients(const size_t index) const
    {
        return coefficients_approximating_polynomial_[index];
    }

    size_t get_substances_count() const
    {
        return coefficients_approximating_polynomial_.size();
    }
};
