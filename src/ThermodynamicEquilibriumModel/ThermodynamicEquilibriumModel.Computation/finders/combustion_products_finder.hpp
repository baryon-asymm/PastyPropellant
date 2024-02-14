#pragma once
#include "../initial_context.hpp"
#include "../solvers/gibbs_energy_solver.hpp"

#include "../../../PastyPropellant.Core.Computatuion/models/constraint.hpp"
#include "../../../PastyPropellant.Core.Computatuion/providers/differential_evolution_provider.hpp"

#pragma managed(push, off)

size_t init_context()
{
    const std::shared_ptr<coefficients_approximating_polynomial> gas_substances = std::make_shared<coefficients_approximating_polynomial>();
    gas_substances->add_substance_coefficients({ 40.782816, -2119.4816, 7476.2362, -1133.0154, 1070.0343, -375.27906, 69.808944, -6.7118939, 0.26221123 });
    gas_substances->add_substance_coefficients({ 53.560373, 7185.7819, 7514.0699, -1193.6487, 1225.4837, -466.28289, 92.951841, -9.5273706, 0.39463884 });
    gas_substances->add_substance_coefficients({ 53.6277, -60008.456, 7369.7266, 504.14536, 1003.769, -481.74652, 102.47379, -10.692381, 0.44473991 });
    gas_substances->add_substance_coefficients({ 52.722143, -1885.3493, 6183.1319, 939.61975, 7.4290406, -98.957196, 29.645176, -3.6804323, 0.17165783 });
    gas_substances->add_substance_coefficients({ 56.654617, 19688.53, 5986.4853, 1702.7013, -548.08811, 99.756451, -9.0098314, 0.23100461, 0.010371999 });
    gas_substances->add_substance_coefficients({ 53.708419, -28282.284, 5982.3212, 1370.6198, -281.80495, 1.6967626, 10.307196, -1.7365992, 0.091892333 });
    gas_substances->add_substance_coefficients({ 54.176023, -96249.681, 5689.7647, 6948.0348, -3187.2165, 907.51942, -154.34752, 14.331037, -0.5571496 });
    gas_substances->add_substance_coefficients({ 52.691784, -24024.488, 6680.2853, -4.416624, 576.48875, -269.37156, 57.845309, -6.1047321, 0.2562386 });
    gas_substances->add_substance_coefficients({ 63.478452, -13372.299, 7951.6749, 1106.6573, -603.89961, 205.22967, -40.460244, 4.4182662, -0.20036230 });
    gas_substances->add_substance_coefficients({ 82.448259, -79591.094, 11647.049, 2411.5284, -1406.5219, 470.36353, -89.764761, 9.0740465, -0.37644653 });
    gas_substances->add_substance_coefficients({ 92.693202, -147586.69, 15979.436, 4067.1232, -2328.2747, 764.61178, -143.40194, 14.260425, -0.58269795 });
    gas_substances->add_substance_coefficients({ 58.519749, 17408.493, 6367.0514, 2505.3047, -1330.6498, 420.95791, -76.919681, 7.5743928, -0.30962628 });
    gas_substances->add_substance_coefficients({ 73.475224, -35689.875, 10192.913, 3849.2796, -2198.8153, 723.29057, -136.16917, 13.608812, -0.55917125 });
    gas_substances->add_substance_coefficients({ 45.168916, 58008.607, 5353.7423, -412.44632, 246.19247, -86.140481, 17.415382, -1.8288189, 0.077299666 });
    gas_substances->add_substance_coefficients({ 33.340205, 50632.635, 4973.3569, -7.0529569, 4.6424756, -1.6298902, 0.31233272, -0.05078677, 0.0012198069 });
    gas_substances->add_substance_coefficients({ 43.651223, 169440.18, 4916.9336, 125.94853, -124.10174, 55.921214, -11.293991, 1.0918904, -0.041516648 });
    gas_substances->add_substance_coefficients({ 42.725638, 111509.41, 5015.7889, -92.295094, 83.547477, -38.540677, 8.9155623, -0.91401847, 0.034707465 });
    gas_substances->add_substance_coefficients({ 45.630131, 74570.055, 5143.7352, -182.22582, 100.85343, -31.783806, 5.8466711, -0.61930206, 0.032455259 });
    gas_substances->add_substance_coefficients({ 45.723654, 27365.638, 5312.55, 305.74464, -357.16969, 150.8185, -32.245532, 3.4771606, -0.15018714 });

    std::shared_ptr<coefficients_approximating_polynomial> liquid_substances = std::make_shared<coefficients_approximating_polynomial>();
    liquid_substances->add_substance_coefficients({ 53.000175, -391289.13, 34599.381, 0.1452368, -0.010641708, 0.0, 0.0, 0.0, 0.0 });

    initial_context::get_instance().gas_substances = gas_substances;
    initial_context::get_instance().liquid_substances = liquid_substances;

    return gas_substances->get_substances_count() + liquid_substances->get_substances_count();
}

#pragma managed

inline double func(const std::vector<double>& args)
{
    return get_gibbs_energy(args, 100 * 101325.0, 3594.0);
}

public ref class combustion_products_finder
{
public:
    combustion_products_finder() {}
    
    void find()
    {
        const double popSize = 100;
        const double procCount = 12;
        const bool minimize = true;

        const size_t substances_count = init_context();

        // 12,3218 0,0679 1,1510 6,6203 0,0071 11,7892 0,1287 1,1081 0,5290 0,1930 0,0036 0,0143 0,1358

        const std::vector<double> args = { 12.3218, 0.0679, 1.1510, 6.6203, 0.0071, 11.7892, 0.1287, 1.1081, 0.5290, 0.1930, 0.0036, 0.0143, 0.1358, 0.0036, 1.4620, 0.0, 0.0, 0.1180, 0.0894, 3.1385 };
        std::cout << "ans: " << func(args);
        return;

        std::vector<std::unique_ptr<constraint>> constraints;
        for (size_t i = 0; i < substances_count; ++i)
            constraints.push_back(std::make_unique<constraint>(1e6, 0.0));

        const differential_evolution_provider differential_evolution_provider(func,
                                                                              popSize,
                                                                              procCount,
                                                                              std::move(constraints),
                                                                              minimize);
        
        const std::vector<double> result = differential_evolution_provider.run();
        for (size_t i = 0; i < result.size(); ++i)
            std::cout << "x" << i << ": " << result.at(i) << " ";
    }
};
