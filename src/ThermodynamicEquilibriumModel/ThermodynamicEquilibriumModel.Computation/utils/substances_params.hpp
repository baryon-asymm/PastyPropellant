#pragma once
#include <vector>

#pragma managed(push, off)

class substance_params
{
private:
    std::vector<double> chemical_elements_molar_masses_;
    std::vector<double> coefficients_approximating_polynomial_;
    
public:
    void set_substance_coefficients(const std::vector<double>& coefficients)
    {
        coefficients_approximating_polynomial_ = coefficients;
    }

    const std::vector<double>& get_substance_coefficients() const
    {
        return coefficients_approximating_polynomial_;
    }

    size_t get_chemical_elements_count() const
    {
        return chemical_elements_molar_masses_.size();
    }
};
