#include "gpu/sampling.h"
#include <cstdio>
#include <limits>

int main() {
    float uv[] = {0,0, 1,0, 0,1, 1,1};
    gpu::Assembled a;
    a.vertex_count = 4;
    a.regs = 1 << 9;
    a.elements[9] = {DXGI_FORMAT_R32G32_FLOAT, 0, 0};
    a.buffers[0] = {reinterpret_cast<const uint8_t*>(uv), sizeof(uv), 8};
    int checks = 0;
    auto expect = [&](bool value) {
        ++checks;
        if (gpu::UnitTexcoords(a) != value) {
            std::printf("FAIL sampling case %d\n", checks);
            return false;
        }
        return true;
    };
    if (!expect(false)) return 1; // world geometry must keep guest addressing
    a.pretransformed = true;
    if (!expect(true)) return 1; // HUD quad keeps the edge workaround
    uv[0] = -0.2f;
    if (!expect(false)) return 1;
    uv[0] = std::numeric_limits<float>::quiet_NaN();
    if (!expect(false)) return 1;
    uv[0] = 0;
    a.buffers[0].bytes = sizeof(uv) - 1;
    if (!expect(false)) return 1;
    a.buffers[0].bytes = sizeof(uv);
    a.elements[9].offset = 4;
    if (!expect(false)) return 1; // offset also counts toward bounds
    a.elements[9].offset = 0;
    a.elements[9].buffer = 16;
    if (!expect(false)) return 1;
    a.elements[9].buffer = 0;
    a.vertex_count = 0;
    if (!expect(false)) return 1;
    std::printf("PASS: %d sampling cases\n", checks);
}
