#include "cpu/x86_ops.h"
#include "kernel/memory.h"
#include "core/log.h"
#include <vector>
#include <atomic>
#include <cstdio>
#include <cstring>

// Temporary invariant tracing. It never changes guest registers or list contents.
namespace {
std::atomic<bool> captured{false};
struct Trace { uint32_t stage, vec, begin, end, caller; };
thread_local Trace history[64]{};
thread_local unsigned cursor=0;
thread_local uint32_t activeVector=0;
thread_local std::vector<uint8_t> previous;
thread_local uint32_t previousBegin=0, previousVector=0;
bool readable(uint32_t p,uint32_t n) {
 if(!n)return true;
 if(p<0x10000 || uint64_t(p)+n>0x100000000ull)return false;
 for(uint64_t q=p&~4095u;q<uint64_t(p)+n;q+=4096)if(!mem::IsCommitted(uint32_t(q)))return false;
 return true;
}
uint32_t read(uint8_t* b,uint32_t p){uint32_t x;std::memcpy(&x,b+p,4);return x;}
void capture(X86Context& c,uint8_t* b,uint32_t stage,uint32_t vec,uint32_t slot,uint32_t object) {
 if(captured.exchange(true))return;
 const auto path=logx::Path()+".render-invalid.txt";
 FILE* f=std::fopen(path.c_str(),"wb");
 if(f){
  std::fprintf(f,"stage=%08x vector=%08x slot=%08x object=%08x thread=%u\n",stage,vec,slot,object,c.thread_handle);
  std::fprintf(f,"eax=%08x ebx=%08x ecx=%08x edx=%08x esi=%08x edi=%08x ebp=%08x esp=%08x last_icall=%08x\n",c.eax,c.ebx,c.ecx,c.edx,c.esi,c.edi,c.ebp,c.esp,c.last_icall);
  for(unsigned i=0;i<64;++i){auto h=history[(cursor+i)%64];std::fprintf(f,"history stage=%08x vec=%08x begin=%08x end=%08x caller=%08x\n",h.stage,h.vec,h.begin,h.end,h.caller);}
  std::fprintf(f,"previous vector=%08x begin=%08x bytes=%zu\n",previousVector,previousBegin,previous.size());
  for(size_t i=0;i+16<=previous.size();i+=16){uint32_t v[4];std::memcpy(v,previous.data()+i,16);std::fprintf(f,"before %08x: %08x %08x %08x %08x\n",previousBegin+uint32_t(i),v[0],v[1],v[2],v[3]);}
  if(readable(vec,12)){uint32_t s=read(b,vec),e=read(b,vec+4);std::fprintf(f,"current header=%08x %08x %08x\n",s,e,read(b,vec+8));
   if(e>=s && e-s<=0x100000 && readable(s,e-s))for(uint32_t p=s;p<e;p+=16)std::fprintf(f,"after %08x: %08x %08x %08x %08x\n",p,read(b,p),read(b,p+4),read(b,p+8),read(b,p+12));
  }
  if(readable(c.esp,256))for(unsigned i=0;i<256;i+=4)std::fprintf(f,"stack %08x: %08x\n",c.esp+i,read(b,c.esp+i));
  std::fclose(f);
 }
 LOGR("[render-trace] FIRST INVALID stage=%08X vector=%08X slot=%08X object=%08X capture=%s",stage,vec,slot,object,path.c_str());logx::Flush();
}
void check(X86Context& c,uint8_t* b,uint32_t stage,uint32_t vec){
 if(captured.load(std::memory_order_relaxed))return;
 if(!readable(vec,12)){capture(c,b,stage,vec,0,0);return;}
 uint32_t s=read(b,vec),e=read(b,vec+4),cap=read(b,vec+8);
 history[cursor++%64]={stage,vec,s,e,readable(c.esp,4)?read(b,c.esp):0};
 if(e<s || cap<e || (e-s)%16 || e-s>0x100000 || !readable(s,e-s)){capture(c,b,stage,vec,0,0);return;}
 for(uint32_t p=s;p<e;p+=16){uint32_t obj=read(b,p);
  if((obj&3) || !readable(obj,0xd4)){capture(c,b,stage,vec,p,obj);return;}
  uint32_t vt=read(b,obj);
  if((vt&3)||!readable(vt,0x58)){capture(c,b,stage,vec,p,obj);return;}
 }
 previous.assign(b+s,b+e);previousBegin=s;previousVector=vec;
}
}
X86_EXTERN_FUNC(__imp__sub_000BFAA0);
X86_FUNC(sub_000BFAA0){uint32_t v=read(base,ctx.esp+12),saved=activeVector;activeVector=v;check(ctx,base,0xBFAA0,v);__imp__sub_000BFAA0(ctx,base);check(ctx,base,0xBFE5E,v);activeVector=saved;}
X86_EXTERN_FUNC(__imp__sub_000C7780);
X86_FUNC(sub_000C7780){uint32_t v=ctx.esi;check(ctx,base,0xC7780,v);__imp__sub_000C7780(ctx,base);check(ctx,base,0xC78A8,v);}
X86_EXTERN_FUNC(__imp__sub_000C3CA0);
X86_FUNC(sub_000C3CA0){uint32_t scene=ctx.ecx;__imp__sub_000C3CA0(ctx,base);if(readable(scene+0x33a8,4))check(ctx,base,0xC3D92,read(base,scene+0x33a8));}
void AzurikRenderCall(X86Context& ctx,uint8_t* base,uint32_t target){
 uint32_t v=activeVector,sp=ctx.esp,ebx=ctx.ebx,esi=ctx.esi,edi=ctx.edi,ebp=ctx.ebp;
 if(v)check(ctx,base,target,v);
 x86_icall(ctx,base,target);
 if(v){
  if(ctx.esp!=sp+12||ctx.ebx!=ebx||ctx.esi!=esi||ctx.edi!=edi||ctx.ebp!=ebp)capture(ctx,base,target,v,ebx,0);
  check(ctx,base,target|0x80000000u,v);
 }
}
void AzurikRenderInvalid(X86Context& ctx,uint8_t* base,uint32_t address,uint32_t object){capture(ctx,base,address,activeVector,ctx.ebx,object);}
