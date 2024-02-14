#pragma once
#include <vector>

#pragma managed(push, off)

class coefficients_approximating_polynomial
{
private:
    std::vector<std::vector<double>> substances_;
    
public:
    void add_substance_coefficients(const std::vector<double>& coefficients)
    {
        substances_.push_back(coefficients);
    }

    const std::vector<double>& get_substance_coefficients(const size_t index)
    {
        return substances_[index];
    }

    size_t get_substances_count() const
    {
        return substances_.size();
    }
};
