#include "cpu/x86_ops.h"
#include <cmath>

// Azurik's _CIasin entry: argument and result in ST(0), no argument pop.
// Its CRT helper at F1ED8 returns ZF, but the generated caller does not
// propagate that flag to F5F2D. A stale ZF sends valid inputs to the NaN path.
// This entry is used by swimming orientation and effect quaternion conversion.
// Match the SDK's _CIasin HLE, which is not bound in this title's symbol map.
X86_FUNC(sub_000F5F10) {
    (void)base;
    X87_ST(0) = std::asin(X87_ST(0));
    ctx.esp += 4;
}
