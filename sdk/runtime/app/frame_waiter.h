#pragma once
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <chrono>
#include <thread>
namespace app {
// Own one timer per pacing thread. Waiting keeps most of the frame cap's idle
// time off CPU; only the final 0.3 ms yields to approach the deadline.
class FrameWaiter {
    HANDLE timer_ = CreateWaitableTimerExW(nullptr,nullptr,0x2,TIMER_MODIFY_STATE|SYNCHRONIZE);
public:
    FrameWaiter(){if(!timer_)timer_=CreateWaitableTimerExW(nullptr,nullptr,0,TIMER_MODIFY_STATE|SYNCHRONIZE);}
    ~FrameWaiter(){if(timer_)CloseHandle(timer_);}
    FrameWaiter(const FrameWaiter&)=delete;
    FrameWaiter& operator=(const FrameWaiter&)=delete;
    void Until(std::chrono::steady_clock::time_point deadline){
        using clock=std::chrono::steady_clock;
        auto remaining=deadline-clock::now()-std::chrono::microseconds(300);
        if(remaining>clock::duration::zero()){
            LARGE_INTEGER due{};
            due.QuadPart=-std::chrono::duration_cast<std::chrono::duration<long long,std::ratio<1,10000000>>>(remaining).count();
            if(due.QuadPart<0 && timer_ && SetWaitableTimer(timer_,&due,0,nullptr,nullptr,FALSE))
                WaitForSingleObject(timer_,INFINITE);
            else while(clock::now()+std::chrono::milliseconds(2)<deadline)Sleep(1);
        }
        while(clock::now()<deadline)std::this_thread::yield();
    }
};
}
