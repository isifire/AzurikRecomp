#pragma once
#include <cstring>
#include <dxgiformat.h>
#include "gpu/vertex.h"

namespace gpu {
// Limit the HUD edge workaround to screen-space vertices. Object-space UVs
// may be animated/transformed by a shader: their raw range cannot justify
// overriding the game's WRAP sampler, even when all currently lie in 0..1.
inline bool UnitTexcoords(const Assembled& a) {
    constexpr float kLo = -1e-4f;
    constexpr float kHi = 1.0001f;
    constexpr uint32_t kSmallDrawVertices = 4096;  // most vertices a small draw has
    if (!a.pretransformed || !a.vertices.empty() || !a.vertex_count || a.vertex_count > kSmallDrawVertices) return false;
    bool any = false;
    for (int r = 0; r < 16; r++) {
        const VertexElement& e = a.elements[size_t(r)];
        if (!(a.regs >> r & 1) || e.format != DXGI_FORMAT_R32G32_FLOAT) continue;
        if (e.buffer >= a.buffers.size()) return false;
        const VertexBuffer& vb = a.buffers[e.buffer];
        if (!vb.src || !vb.stride || uint64_t(a.vertex_count - 1) * vb.stride + e.offset + sizeof(float) * 2 > vb.bytes) return false;
        for (uint32_t v = 0; v < a.vertex_count; v++) {
            float uv[2];
            std::memcpy(uv, vb.src + size_t(v) * vb.stride + e.offset, sizeof(uv));
            if (!(uv[0] >= kLo && uv[0] <= kHi && uv[1] >= kLo && uv[1] <= kHi)) return false;
        }
        any = true;
    }
    return any;
}

} // namespace gpu
