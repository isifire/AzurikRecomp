// the settings file beside the exe
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>

#include <algorithm>
#include <atomic>
#include <fstream>
#include <sstream>

#include <toml++/toml.hpp>

#include "app/app.h"
#include "core/log.h"

namespace app {

namespace {

const char* kModes[] = {"fullscreen", "borderless", "windowed"};

std::filesystem::path File() { return ExeDir() / kGame.settings; }

}  // namespace

Settings& Get() {
    static Settings s;
    return s;
}

void Load() {
    Settings& s = Get();
    toml::table t;
    try {
        t = toml::parse_file(File().string());
    } catch (...) {
        return;
    }
    std::string mode = t["display_mode"].value_or(std::string(kModes[uint32_t(s.display_mode)]));
    for (uint32_t i = 0; i < 3; i++)
        if (mode == kModes[i]) s.display_mode = DisplayMode(i);
    s.width = t["width"].value_or(s.width);
    s.height = t["height"].value_or(s.height);
    s.render_percent = t["render_percent"].value_or(s.render_percent);
    s.fps_cap = t["fps_cap"].value_or(s.fps_cap);
    s.vsync = t["vsync"].value_or(s.vsync);
    s.aniso = t["anisotropic_filtering"].value_or(s.aniso);
    s.post_aa = t["post_antialiasing"].value_or(s.post_aa);
    s.post_sharpen = std::min<uint32_t>(100, t["post_sharpen"].value_or(s.post_sharpen));
    s.post_brightness = std::clamp(t["post_brightness"].value_or(s.post_brightness), 50, 150);
    s.post_contrast = std::clamp(t["post_contrast"].value_or(s.post_contrast), 50, 150);
    s.post_saturation = std::clamp(t["post_saturation"].value_or(s.post_saturation), 0, 150);
    s.nearest_upscale = t["nearest_upscale"].value_or(s.nearest_upscale);
    s.upscale_filter = std::clamp(t["upscale_filter"].value_or(s.nearest_upscale ? 1 : 0), 0, 2);
    s.diagnostic_frames = t["diagnostic_frames"].value_or(false);
    s.large_text = t["large_text"].value_or(false);
    s.join_ip = t["join_ip"].value_or(s.join_ip);
    s.game_language = t["game_language"].value_or(std::string("en"));
    if (s.game_language != "es-ES") s.game_language = "en";
    for (const Option& o : kGame.options) {
        if (o.flag) *o.flag = t[o.key].value_or(*o.flag);
        else *o.value = t[o.key].value_or(*o.value);
    }
    if (kGame.load) kGame.load(&t);
}

namespace {

toml::table Table() {
    const Settings& s = Get();
    toml::table t{{"display_mode", kModes[uint32_t(s.display_mode)]},
                  {"width", int64_t(s.width)},
                  {"height", int64_t(s.height)},
                  {"render_percent", int64_t(s.render_percent)},
                  {"fps_cap", int64_t(s.fps_cap)},
                  {"vsync", s.vsync},
                  {"anisotropic_filtering", int64_t(s.aniso)},
                  {"post_antialiasing", s.post_aa},
                  {"post_sharpen", int64_t(s.post_sharpen)},
                  {"post_brightness", s.post_brightness},
                  {"post_contrast", s.post_contrast},
                  {"post_saturation", s.post_saturation},
                  {"upscale_filter", s.upscale_filter},
                  {"nearest_upscale", s.nearest_upscale},
                  {"diagnostic_frames", s.diagnostic_frames},
                  {"large_text", s.large_text},
                  {"join_ip", s.join_ip},
                  {"game_language", s.game_language}};
    for (const Option& o : kGame.options) {
        if (o.flag) t.insert(o.key, *o.flag);
        else t.insert(o.key, int64_t(*o.value));
    }
    if (kGame.save) kGame.save(&t);
    return t;
}

}  // namespace

void Save() {
    std::ofstream f(File());
    f << Table() << "\n";
}

void LogSettings() {
    std::ostringstream text;
    text << Table();
    std::istringstream lines(text.str());
    LOGR("Settings:");
    for (std::string line; std::getline(lines, line);)
        if (!line.empty()) LOGR("  %s", line.c_str());
}

Mode Desktop() {
    HMONITOR mon = MonitorFromWindow(static_cast<HWND>(Window()), MONITOR_DEFAULTTOPRIMARY);
    // RenderScale is queried every frame. Display enumeration can enter the driver;
    // cache per thread for at most one second, refreshing immediately on monitor changes.
    thread_local HMONITOR cached_monitor = nullptr;
    thread_local Mode cached_mode{};
    thread_local ULONGLONG refreshed = 0;
    const ULONGLONG now = GetTickCount64();
    if (mon == cached_monitor && cached_mode.width && now - refreshed < 1000)
        return cached_mode;
    MONITORINFOEXW mi{};
    mi.cbSize = sizeof(mi);
    DEVMODEW dm{};
    dm.dmSize = sizeof(dm);
    if (GetMonitorInfoW(mon, &mi) && EnumDisplaySettingsW(mi.szDevice, ENUM_REGISTRY_SETTINGS, &dm))
        cached_mode = {dm.dmPelsWidth, dm.dmPelsHeight};
    else
        cached_mode = {uint32_t(GetSystemMetrics(SM_CXSCREEN)), uint32_t(GetSystemMetrics(SM_CYSCREEN))};
    cached_monitor = mon;
    refreshed = now;
    return cached_mode;
}

Mode Output() {
    const Settings& s = Get();
    if (s.width && s.height) return {s.width, s.height};
    return Desktop();
}

bool Widescreen() {
    // asked per frame and per HUD quad: the desktop mode lookup only runs when the resolution setting changes
    static std::atomic<uint64_t> key{~0ull};
    static std::atomic<bool> wide{true};
    const Settings& s = Get();
    uint64_t k = uint64_t(s.width) << 32 | s.height;
    if (key.exchange(k) != k) {
        Mode m = Output();
        wide = m.width * 2 >= m.height * 3;  // 3:2 or wider
    }
    return wide;
}

float RenderScale(uint32_t guest_height) {
    float h = Output().height * (Get().render_percent / 100.0f);
    return guest_height ? std::max(h / guest_height, 0.1f) : 1.0f;
}

}  // namespace app
