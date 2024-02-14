#pragma once

#include "../../../libs/DifferentialEvolution/processors.hpp"

class base_processor_listener : public amichel::de::processor_listener
{
private:
    amichel::de::mutex mutex;

public:
    virtual void start(size_t index)
    {
        amichel::de::lock lock(mutex);
    }
    
    virtual void start_of(size_t index, amichel::de::individual_ptr ind)
    {
        amichel::de::lock lock(mutex);
    }

    virtual void end_of(size_t index, amichel::de::individual_ptr ind)
    {
        amichel::de::lock lock(mutex);
    }

    virtual void end(size_t index)
    {
        amichel::de::lock lock(mutex);
    }

    virtual void error(size_t index, const std::string& message)
    {
        amichel::de::lock lock(mutex);
    }
};
