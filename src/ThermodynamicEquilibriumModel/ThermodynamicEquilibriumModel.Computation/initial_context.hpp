#pragma once
#include <memory>

#include "utils/coefficients_approximating_polynomial.hpp"

#pragma managed(push, off)

class initial_context
{
private:
    initial_context() {}
    
    initial_context(initial_context const&);
    void operator=(initial_context const&);

public:
    static initial_context& get_instance()
    {
        static initial_context instance;
        return instance;
    }
    
    std::shared_ptr<coefficients_approximating_polynomial> gas_substances;
    std::shared_ptr<coefficients_approximating_polynomial> liquid_substances;
};
