#pragma once
#include "../../../libs/DifferentialEvolution/differential_evolution.hpp"

#include "../listeners/base_listener.hpp"
#include "../processor_listeners/base_processor_listener.hpp"
#include "../models/constraint.hpp"

class differential_evolution_provider
{
    std::unique_ptr<amichel::de::differential_evolution> differential_evolution_solver;
    std::unique_ptr<amichel::de::processors> processors;
    std::unique_ptr<amichel::de::constraints> de_constraints;
    std::unique_ptr<amichel::de::termination_strategy> termination_strategy;
    std::unique_ptr<amichel::de::selection_strategy> selection_strategy;

public:
    differential_evolution_provider(const std::function<double(const std::vector<double>&)>& objective_function,
                                    size_t popSize,
                                    size_t procCount,
                                    const std::vector<std::unique_ptr<constraint>> constraints,
                                    bool minimize,
                                    size_t maxGenerations = 10000,
                                    double weight = 0.7,
                                    double crossover = 0.9)
    {
        amichel::de::processor_listener_ptr processor_listener = std::make_shared<
            amichel::de::null_processor_listener>();

        processors = std::make_unique<amichel::de::processors>(procCount, objective_function, processor_listener);

        double defMin = -1.0, defMax = 1.0;
        de_constraints = std::make_unique<amichel::de::constraints>(constraints.size(), defMin, defMax);
        for (size_t i = 0; i < constraints.size(); ++i)
            de_constraints->operator[](i) = std::make_shared<amichel::de::real_constraint>(
                constraints[i]->get_min(), constraints[i]->get_max());

        termination_strategy = std::make_unique<amichel::de::termination_strategy>(
            amichel::de::max_gen_termination_strategy{maxGenerations});

        selection_strategy = std::make_unique<amichel::de::selection_strategy>(
            amichel::de::best_parent_child_selection_strategy{});

        amichel::de::mutation_strategy_arguments mutation_arguments(weight, crossover);
        amichel::de::mutation_strategy_ptr mutation_strategy(
            std::make_shared<amichel::de::mutation_strategy_1>(constraints.size(), mutation_arguments));

        amichel::de::listener_ptr listener(std::make_shared<base_listener>());

        differential_evolution_solver = std::make_unique<amichel::de::differential_evolution>(constraints.size(),
            popSize,
            *processors,
            *de_constraints,
            minimize,
            *termination_strategy,
            *selection_strategy,
            mutation_strategy,
            listener);
    }

    std::vector<double> run() const
    {
        differential_evolution_solver->run();
        amichel::de::individual_ptr result = differential_evolution_solver->best();
        return result->vars();
    }

    differential_evolution_provider(const differential_evolution_provider&) = delete;
    differential_evolution_provider& operator=(const differential_evolution_provider&) = delete;
};
