#pragma once

#include "../../../libs/DifferentialEvolution/processors.hpp"

class base_processor_listener : public amichel::de::processor_listener
{
    amichel::de::mutex mutex;

public:
    void start(size_t index) override
    {
        amichel::de::lock lock(mutex);
    }

    void start_of(size_t index, amichel::de::individual_ptr ind) override
    {
        amichel::de::lock lock(mutex);
    }

    void end_of(size_t index, amichel::de::individual_ptr ind) override
    {
        amichel::de::lock lock(mutex);
    }

    void end(size_t index) override
    {
        amichel::de::lock lock(mutex);
    }

    void error(size_t index, const std::string& message) override
    {
        amichel::de::lock lock(mutex);
    }
};
