#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <algorithm>
#include <mutex>
#include <string>
#include <cstdlib>
#include "app/app.h"
#include <atomic>
#include <cstdint>
#include <chrono>
#include "core/input.h"
#include "pad_mapping.h"
#include "core/log.h"

namespace {
unsigned bindings[24] = {0x26,0x28,0x25,0x27,0x0D,0x08,'C','V',0x20,'E','Q','R','X','Z',0x10,0x11,'D','A','W','S','L','J','I','K'};
std::array<int,16> pad_map = {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15};
int deadzone = 6000;
bool invert_camera_horizontal = true, invert_camera_vertical = false;
int sensitivity_x = 100, sensitivity_y = 100, vibration = 100;
std::once_flag loaded;
void LoadBindings() {
    const auto file = (app::ExeDir() / "../../../controles.ini").lexically_normal().string();
    const char* names[] = {"dpad_up","dpad_down","dpad_left","dpad_right","start","back","left_click","right_click","a","b","x","y","black","white","left_trigger","right_trigger","move_right","move_left","move_forward","move_back","camera_right","camera_left","camera_up","camera_down"};
    for (int i=0;i<24;++i) {
        char value[64]{};
        GetPrivateProfileStringA("keyboard", names[i], "", value, sizeof(value), file.c_str());
        if (!value[0]) continue;
        std::string v(value);
        for (auto& c : v) if (c >= 'a' && c <= 'z') c -= 32;
        unsigned key = 256;
        if (v.size()==1 && ((v[0]>='A'&&v[0]<='Z')||(v[0]>='0'&&v[0]<='9'))) key=v[0];
        struct Named { const char* name; unsigned key; };
        const Named keys[] = {{"SPACE",32},{"ENTER",13},{"BACKSPACE",8},{"TAB",9},{"SHIFT",16},{"CTRL",17},{"ALT",18},{"UP",38},{"DOWN",40},{"LEFT",37},{"RIGHT",39},{"NONE",0}};
        for (auto n : keys) if (v==n.name) key=n.key;
        if (v.rfind("0X",0)==0) { char* end=nullptr; unsigned long parsed=std::strtoul(v.c_str(),&end,16); if(end && !*end && parsed<256) key=unsigned(parsed); }
        if(key<256) bindings[i]=key;
        else LOGI("keyboard: invalid binding %s=%s; keeping default",names[i],value);
    }
    for(int i=0;i<16;++i) pad_map[i]=std::clamp(int(GetPrivateProfileIntA("gamepad",names[i],i,file.c_str())),-1,15);
    deadzone = std::clamp(int(GetPrivateProfileIntA("controller","deadzone",6000,file.c_str())),0,30000);
    invert_camera_horizontal = GetPrivateProfileIntA("camera","invert_horizontal",1,file.c_str()) != 0;
    invert_camera_vertical = GetPrivateProfileIntA("camera","invert_vertical",0,file.c_str()) != 0;
    sensitivity_x = std::clamp(int(GetPrivateProfileIntA("camera","sensitivity_horizontal",100,file.c_str())),25,200);
    sensitivity_y = std::clamp(int(GetPrivateProfileIntA("camera","sensitivity_vertical",100,file.c_str())),25,200);
    vibration = std::clamp(int(GetPrivateProfileIntA("controller","vibration",100,file.c_str())),0,100);
    LOGI("input: keyboard mappings loaded from %s",file.c_str());
}
std::atomic_bool keys[256]{};
std::atomic<int64_t> pressed_until[256]{};
int64_t Now() {
    return std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::steady_clock::now().time_since_epoch()).count();
}
bool Down(unsigned key, int64_t now) {
    if (!key || key >= 256) return false;
    return keys[key].load(std::memory_order_relaxed) ||
           pressed_until[key].load(std::memory_order_relaxed) > now;
}
}

bool AzurikKeyboardMessage(void*, uint32_t msg, uintptr_t wp, intptr_t, intptr_t*) {
    if ((msg == WM_KEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP) && wp < 256) {
        const bool down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
        if (down) LOG_FIRST(16, "keyboard: key %u", unsigned(wp));
        keys[wp].store(down, std::memory_order_relaxed);
        // Preserve quick taps until the game's next 30 Hz controller sample.
        if (down) pressed_until[wp].store(Now() + 80, std::memory_order_relaxed);
    }
    if (msg == 0x8) { // WM_KILLFOCUS
        for (auto& key : keys) key.store(false, std::memory_order_relaxed);
        for (auto& until : pressed_until) until.store(0, std::memory_order_relaxed);
    }
    return false;
}

void AzurikKeyboardPad(int port, input::Pad& pad) {
    if (port != 0) return;
    std::call_once(loaded, LoadBindings);
    RemapPad(pad,pad_map);
    auto filter = [](int16_t v) { int n=v; int m=std::abs(n); return int16_t(m<=deadzone ? 0 : (n<0?-1:1)*std::min(32767,(m-deadzone)*32767/(32767-deadzone))); };
    pad.lx=filter(pad.lx); pad.ly=filter(pad.ly); pad.rx=filter(pad.rx); pad.ry=filter(pad.ry);
    const auto now = Now();
    pad.connected = true;
    for (int i=0;i<8;++i) if (Down(bindings[i], now)) pad.buttons |= uint16_t(1u<<i);
    for (int i=0;i<8;++i) if (Down(bindings[8+i], now)) pad.analog[i] = 255;
    int16_t* axes[] = {&pad.lx,&pad.ly,&pad.rx,&pad.ry};
    for(int i=0;i<4;++i) {
        bool positive=Down(bindings[16+i*2], now), negative=Down(bindings[17+i*2], now);
        if(positive||negative) *axes[i]=int16_t((int(positive)-int(negative))*32767);
    }
    pad.rx = int16_t(std::clamp(int(pad.rx) * sensitivity_x / 100 * (invert_camera_horizontal ? -1 : 1), -32768, 32767));
    pad.ry = int16_t(std::clamp(int(pad.ry) * sensitivity_y / 100 * (invert_camera_vertical ? -1 : 1), -32768, 32767));
}

void AzurikRumble(int port, uint16_t& left, uint16_t& right) {
    if (port != 0) return;
    std::call_once(loaded, LoadBindings);
    left = uint16_t(uint32_t(left) * vibration / 100);
    right = uint16_t(uint32_t(right) * vibration / 100);
}
