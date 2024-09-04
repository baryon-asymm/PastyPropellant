#pragma once
#include <vector>
#include <memory>

#include "../../../../PastyPropellant.Core.Computatuion/models/constraint.hpp"
#include "../../../../PastyPropellant.Core.Computatuion/providers/differential_evolution_provider.hpp"

#pragma managed

// min: -1.031628453
inline double SixHumpCamelBackFunction(const std::vector<double>& args)
{
    double x1(args.at(0));
    double x2(args.at(1));

    return (4.0 - 2.1 * x1 * x1 + pow(x1, 4) / 3) * x1 * x1 + x1 * x2 + (-4.0 + 4.0 * x2 * x2) * x2 * x2;
}

public ref class test_solver
{
public:
    test_solver()
    {
    }

    void test()
    {
        constexpr double popSize = 100;
        constexpr double procCount = 6;
        constexpr bool minimize = true;

        double x1_min = -5.0, x1_max = 5.0;
        double x2_min = -5.0, x2_max = 5.0;

        std::vector<std::unique_ptr<constraint>> constraints;
        constraints.push_back(std::make_unique<constraint>(x1_max, x1_min));
        constraints.push_back(std::make_unique<constraint>(x2_max, x2_min));

        const differential_evolution_provider differential_evolution_provider(SixHumpCamelBackFunction,
                                                                              popSize,
                                                                              procCount,
                                                                              std::move(constraints),
                                                                              minimize);
        const std::vector<double> result = differential_evolution_provider.run();

        std::cout << "x1: " << result.at(0) << " x2: " << result.at(1) << " cost: " << SixHumpCamelBackFunction(result);
    }
};
