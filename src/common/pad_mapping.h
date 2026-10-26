#pragma once
#include <array>
#include "core/input.h"
// Each virtual button selects one physical input; read from a snapshot so swaps
// and duplicate assignments do not depend on iteration order. -1 disables it.
inline void RemapPad(input::Pad& pad, const std::array<int,16>& map) {
    const auto source = pad;
    pad.buttons = 0;
    for(int target=0;target<16;++target) {
        int from=map[target];
        unsigned value=from<0 || from>=16 ? 0 : from<8 ? ((source.buttons>>from)&1)*255 : source.analog[from-8];
        if(target<8) { if(value>=128)pad.buttons|=uint16_t(1u<<target); }
        else pad.analog[target-8]=uint8_t(value);
    }
}
