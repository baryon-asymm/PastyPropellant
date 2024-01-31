#include "differential_evolution_provider.h"

template <typename T>
differential_evolution_provider<T>::differential_evolution_provider(std::function<double(std::vector<double>&&)> objective_function,
                                                                    size_t varCount,
                                                                    size_t popSize,
                                                                    size_t procCount,
                                                                    const std::unique_ptr<T> constraints,
                                                                    bool minimize)
{
    //amichel::de::processors& processors = new amichel::de::processors(procCount,
    //                                                                  objective_function,
    //                                                                  )
    
    //differential_evolution_solver = std::make_unique<amichel::de::differential_evolution>(varCount,
    //                                                                                      popSize,
    //                                                                                      
}
