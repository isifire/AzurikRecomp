#include "cpu/x86_ops.h"
#include "kernel/memory.h"
#include "core/log.h"
#include <cmath>
#include <cstdint>
#include <cstring>
#include <mutex>
#include <set>

namespace {
int BadFloat(uint8_t* base, uint32_t address) {
    if (address < 0x10000 || address > 0xffffffc0 ||
        !mem::IsCommitted(address) || !mem::IsCommitted(address + 63)) return -2;
    const float* values = reinterpret_cast<const float*>(base + address);
    for (int i = 0; i < 16; ++i) if (!std::isfinite(values[i])) return i;
    return -1;
}
void Report(uint8_t* base, const char* stage, uint32_t caller, uint32_t source, uint32_t dest) {
    int from = BadFloat(base, source), to = BadFloat(base, dest);
    if (from == -1 && to == -1) return;
    LOG_FIRST(8, "water-matrix: %s caller=%08X src=%08X bad=%d dst=%08X bad=%d", stage, caller,
              source, from, dest, to);
}
}
X86_EXTERN_FUNC(__imp__sub_0008DE10);
X86_FUNC(sub_0008DE10) {
    const uint32_t source=ctx.eax, caller=X86_MEM32(ctx.esp);
    __imp__sub_0008DE10(ctx,base);
    Report(base,"object-world",caller,source,0x387B00);
}
X86_EXTERN_FUNC(__imp__sub_0008DE30);
X86_FUNC(sub_0008DE30) {
    const uint32_t source=ctx.eax, caller=X86_MEM32(ctx.esp);
    __imp__sub_0008DE30(ctx,base);
    Report(base,"camera-view",caller,source,0x387890);
}
X86_EXTERN_FUNC(__imp__sub_0009A7F0);
X86_FUNC(sub_0009A7F0) {
    const uint32_t caller=X86_MEM32(ctx.esp);
    const int view=BadFloat(base,0x387B00),projection=BadFloat(base,0x387890);
    __imp__sub_0009A7F0(ctx,base);
    const int result=BadFloat(base,0x380800);
    if(view!=-1 || projection!=-1 || result!=-1)
        LOG_FIRST(8,"water-matrix: build caller=%08X worldBad=%d cameraViewBad=%d resultBad=%d",caller,view,projection,result);
}

X86_EXTERN_FUNC(__imp__sub_0008E330);
X86_FUNC(sub_0008E330) {
    const uint32_t q=ctx.eax,p=ctx.edx,caller=X86_MEM32(ctx.esp);
    float position[3],rotation[4];
    std::memcpy(position,base+p,sizeof(position));
    std::memcpy(rotation,base+q,sizeof(rotation));
    __imp__sub_0008E330(ctx,base);
    if(BadFloat(base,0x387890)!=-1)
        LOG_FIRST(12,"water-camera-input: caller=%08X posAt=%08X xyz=%g,%g,%g quatAt=%08X xyzw=%g,%g,%g,%g",
                  caller,p,position[0],position[1],position[2],q,rotation[0],rotation[1],rotation[2],rotation[3]);
}

// Log the arithmetic instruction that first creates a non-finite value,
// not the later matrix copy that merely propagates it. No guest state changes.
void AzurikInvalidArithmetic(X86Context& ctx,uint8_t* base,uint32_t pc,double a,double b,double result) {
    static std::mutex lock;
    static std::set<uint32_t> reported;
    std::lock_guard<std::mutex> guard(lock);
    if(reported.size()>=64 || !reported.insert(pc).second)return;
    LOGR("[float-origin] pc=%08X a=%.17g b=%.17g result=%.17g eax=%08X ebx=%08X ecx=%08X edx=%08X esi=%08X edi=%08X ebp=%08X esp=%08X last_icall=%08X",
         pc,a,b,result,ctx.eax,ctx.ebx,ctx.ecx,ctx.edx,ctx.esi,ctx.edi,ctx.ebp,ctx.esp,ctx.last_icall);
    if(mem::IsCommitted(ctx.esp) && mem::IsCommitted(ctx.esp+127))
        for(unsigned i=0;i<32;i+=4)
            LOGR("[float-stack] pc=%08X at=%08X %08X %08X %08X %08X",pc,ctx.esp+i*4,
                 X86_MEM32(ctx.esp+i*4),X86_MEM32(ctx.esp+i*4+4),X86_MEM32(ctx.esp+i*4+8),X86_MEM32(ctx.esp+i*4+12));
    logx::Flush();
}

void AzurikInvalidStore(X86Context& ctx,uint8_t* base,uint32_t pc,uint32_t address,double value) {
    static std::mutex lock;
    static std::set<uint32_t> reported;
    std::lock_guard<std::mutex> guard(lock);
    if(reported.size()>=256 || !reported.insert(pc).second)return;
    LOGR("[float-store] pc=%08X dst=%08X value=%.17g eax=%08X ebx=%08X ecx=%08X edx=%08X esi=%08X edi=%08X ebp=%08X esp=%08X last_icall=%08X",
         pc,address,value,ctx.eax,ctx.ebx,ctx.ecx,ctx.edx,ctx.esi,ctx.edi,ctx.ebp,ctx.esp,ctx.last_icall);
    if(mem::IsCommitted(ctx.esp) && mem::IsCommitted(ctx.esp+31))
        LOGR("[float-store-stack] pc=%08X %08X %08X %08X %08X %08X %08X %08X %08X",pc,
             X86_MEM32(ctx.esp),X86_MEM32(ctx.esp+4),X86_MEM32(ctx.esp+8),X86_MEM32(ctx.esp+12),
             X86_MEM32(ctx.esp+16),X86_MEM32(ctx.esp+20),X86_MEM32(ctx.esp+24),X86_MEM32(ctx.esp+28));
    logx::Flush();
}
