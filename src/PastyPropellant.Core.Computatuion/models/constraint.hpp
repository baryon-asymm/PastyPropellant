#pragma once

class constraint
{
private:
    double max;
    double min;

public:
    constraint(double max, double min) : max(max), min(min) {}
    
    double get_max() const
    {
        return max;
    }
    
    double get_min() const
    {
        return min;
    }
};
