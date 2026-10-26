#pragma once
#include <algorithm>
#include <array>
#include <cmath>
#include <cstddef>
namespace gpu {
struct FrameStats {
    std::array<double,4096> values{};
    size_t count=0, over50=0, over100=0;
    void Add(double ms) {
        if(!std::isfinite(ms) || ms<0)return;
        if(count==values.size())return;
        values[count++]=ms;over50+=ms>50;over100+=ms>100;
    }
    void Sort(){std::sort(values.begin(),values.begin()+count);}
    double Percentile(double p) const {
        if(!count)return 0;
        const size_t n=size_t(std::ceil(std::clamp(p,0.0,1.0)*count));
        return values[n?n-1:0];
    }
};
}
