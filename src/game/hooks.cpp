#include "xdk/hle.h"

// IDirectSound_GetCaps: the host mixer replaces the DS object. The original
// function would dereference Xbox driver fields in that host object's storage.
// Four DWORD output fields confirmed at 0x13581A and 0x1355CE.
X86_FUNC(sub_00136AB6) {
    uint32_t caps = kernel::R32(ctx.esp + 8);
    if (caps) {
        kernel::W32(caps, 256);     // free 2D voices
        kernel::W32(caps + 4, 64);  // free 3D voices
        kernel::W32(caps + 8, 2047);// free scatter/gather entries
        kernel::W32(caps + 12, 0); // no Xbox hardware voice memory allocated
    }
    kernel::Return(ctx, 0, 2);
}
