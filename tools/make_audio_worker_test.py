"""Emit an isolated regression harness from the actual patched audio worker."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'generated/game/x86_recomp.28.cpp').read_text()
s=s.split('X86_FUNC(__imp__sub_000AE450) {',1)[1].split('X86_WEAK_FUNC(sub_000AE450',1)[0]
prefix='''#include "cpu/x86_ops.h"
#include <vector>
#include <cstdio>
#include <stdexcept>
X86_EXTERN_FUNC(sub_000E11EC);
X86_EXTERN_FUNC(sub_000E0D80);
X86_EXTERN_FUNC(sub_000ACF20);
'''
suffix=r'''
static int waits,locked,calls,mode;
struct Finished {};
X86_FUNC(sub_000E11EC){if(waits++)throw Finished{};ctx.esp+=12;}
X86_FUNC(sub_000E0D80){ctx.esp+=8;}
X86_FUNC(sub_000ACF20){ctx.esp+=4;}
void x86_icall(X86Context& ctx,uint8_t* base,uint32_t target){
 if(target==1){++locked;ctx.esp+=8;return;}
 if(target==2){--locked;ctx.esp+=8;return;}
 if(target!=3)throw std::runtime_error("bad dispatch");
 if(++calls>8)throw std::runtime_error("infinite traversal");
 if(mode==2){uint32_t n=ctx.ecx+4;X86_MEM32(n)=n;X86_MEM32(n+4)=n;}
 ctx.esp+=4;
}
int main(){
 for(mode=0;mode<4;++mode){
 std::vector<uint8_t> mem(0x200000);auto base=mem.data();X86Context ctx{};ctx.esp=0x1f0000;
 waits=locked=calls=0;
 X86_MEM32(0x18f51c)=1;X86_MEM32(0x18f514)=2;
 const uint32_t head=0x1aa7d8,node=0x100004,next=0x100104;
 X86_MEM32(head)=mode==3?head:node;
 X86_MEM32(node-4)=X86_MEM32(next-4)=0x110000;X86_MEM32(0x110040)=3;
 X86_MEM32(node)=mode==1?node:next;X86_MEM32(node+4)=mode==1?node:head;
 X86_MEM32(next)=head;X86_MEM32(next+4)=node;
 try{sub_000AE450(ctx,base);}catch(const Finished&){}
 if(locked || X86_MEM8(0x1bf57c)!=0 || calls!=(mode==0?2:mode==2?1:0))return 1;
 }
 puts("PASS: normal and empty lists, retired node before callback, retirement during callback; lock released in all cases");
}
'''
out=root/'out/audio_worker_test.cpp';out.write_text(prefix+'X86_FUNC(sub_000AE450) {'+s+suffix)
print(out)
