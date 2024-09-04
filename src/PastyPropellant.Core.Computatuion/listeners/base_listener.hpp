#pragma once
#include <iostream>

#include "../../../libs/DifferentialEvolution/listener.hpp"

class base_listener : public amichel::de::listener
{
    double lastBestCost = 0;

public:
    void start() override
    {
    }

    void end() override
    {
    }

    void error() override
    {
    }

    void startGeneration(size_t genCount) override
    {
    }

    void endGeneration(size_t genCount,
                       amichel::de::individual_ptr bestIndGen,
                       amichel::de::individual_ptr bestInd) override
    {
        if (lastBestCost == bestInd->cost())
            return;
        lastBestCost = bestInd->cost();
        std::cout << (boost::format("genCount: %1%, cost: %2%\n") % genCount % bestInd->cost()).str();
    }

    void startSelection(size_t genCount) override
    {
    }

    void endSelection(size_t genCount) override
    {
    }

    void startProcessors(size_t genCount) override
    {
    }

    void endProcessors(size_t genCount) override
    {
    }
};
