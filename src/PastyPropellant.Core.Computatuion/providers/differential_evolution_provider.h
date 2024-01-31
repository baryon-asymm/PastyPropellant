#pragma once
#include "../../../libs/DifferentialEvolution/differentialevolution/differential_evolution.hpp"

template <typename T>
class differential_evolution_provider
{
private:
    const std::unique_ptr<amichel::de::differential_evolution> differential_evolution_solver;

public:
    differential_evolution_provider(std::function<double(std::vector<double>&&)> objective_function,
                                    size_t varCount,
                                    size_t popSize,
                                    size_t procCount,
                                    const std::unique_ptr<T> constraints,
                                    bool minimize);
    std::vector<double>&& run();
    
    differential_evolution_provider(const differential_evolution_provider&) = delete;
    differential_evolution_provider& operator=(const differential_evolution_provider&) = delete;
};
