#include "gpu/frame_stats.h"
#include "../src/common/pad_mapping.h"
#include <cstdio>
int main(){
    gpu::FrameStats s;for(int i=1;i<=100;i++)s.Add(i);s.Add(200);s.Sort();
    if(s.Percentile(.5)!=51 || s.Percentile(.95)!=96 || s.Percentile(.99)!=100 || s.Percentile(1)!=200 || s.over50!=51 || s.over100!=1)return 1;
    s={};s.Sort();if(s.Percentile(.99)!=0)return 2;
    std::array<int,16> map;for(int i=0;i<16;i++)map[i]=i;
    input::Pad p;p.buttons=4;p.analog[0]=199;p.analog[1]=23;p.lx=123;
    RemapPad(p,map);if(p.buttons!=4||p.analog[0]!=199||p.lx!=123)return 3;
    map[8]=9;map[9]=8;RemapPad(p,map);if(p.analog[0]!=23||p.analog[1]!=199)return 4;
    map[8]=-1;map[9]=2;map[0]=9;RemapPad(p,map);if(p.analog[0]!=0||p.analog[1]!=255||!(p.buttons&1))return 5;
    puts("PASS: frame percentiles, empty series, controller identity, swaps, disabled and digital/analog mappings");
}
