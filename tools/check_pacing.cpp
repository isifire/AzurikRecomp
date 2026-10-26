#include "app/frame_waiter.h"
#include <cstdio>
int main(){using clock=std::chrono::steady_clock;app::FrameWaiter w;auto start=clock::now();double maxLate=0;
for(int i=1;i<=120;i++){auto end=start+std::chrono::microseconds(i*16667LL);w.Until(end);double late=std::chrono::duration<double,std::milli>(clock::now()-end).count();if(late<0)return 1;if(late>maxLate)maxLate=late;}
w.Until(clock::now()-std::chrono::seconds(1));std::printf("PASS: 120 deadlines, no early return; max lateness %.3f ms (host scheduling sample, not gameplay)\n",maxLate);}
