#pragma once
#include <array>
#include <chrono>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <vector>
#ifdef _WIN32
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#endif

namespace gpu::shader_cache {
constexpr uint64_t kMagic = 0x3145484341435341ull; // ASCACHE1
constexpr size_t kMaxBytes = 8 * 1024 * 1024;
inline uint64_t Checksum(const void* bytes, size_t size) {
    auto p = static_cast<const uint8_t*>(bytes);
    uint64_t h = 14695981039346656037ull;
    for (size_t i = 0; i < size; ++i) h = (h ^ p[i]) * 1099511628211ull;
    return h;
}
inline std::filesystem::path File(const std::filesystem::path& dir, uint64_t key) {
    return dir / (std::to_string(key) + ".bin");
}
inline std::vector<uint8_t> Load(const std::filesystem::path& dir, uint64_t key) {
    std::ifstream f(File(dir, key), std::ios::binary | std::ios::ate);
    if (!f) return {};
    auto length = f.tellg();
    if (length < 64 || length > std::streamoff(kMaxBytes + 32)) return {};
    f.seekg(0);
    std::array<uint64_t, 4> header{};
    if (!f.read(reinterpret_cast<char*>(header.data()), 32)) return {};
    if (header[0] != kMagic || header[1] != key || header[2] != uint64_t(length) - 32) return {};
    std::vector<uint8_t> data(static_cast<size_t>(header[2]));
    if (!f.read(reinterpret_cast<char*>(data.data()), std::streamsize(data.size()))) return {};
    if (std::memcmp(data.data(), "DXBC", 4) || Checksum(data.data(), data.size()) != header[3]) return {};
    return data;
}
inline void Store(const std::filesystem::path& dir, uint64_t key, const void* bytes, size_t size, uint32_t process) {
    if (size < 32 || size > kMaxBytes || std::memcmp(bytes, "DXBC", 4)) return;
    std::error_code ec;
    std::filesystem::create_directories(dir, ec);
    if (ec) return; // caching is optional, including on read-only installations
    auto dest = File(dir, key), temp = dest;
    temp += "." + std::to_string(process) + "." +
            std::to_string(std::chrono::steady_clock::now().time_since_epoch().count()) + ".tmp";
    std::array<uint64_t, 4> header{kMagic, key, uint64_t(size), Checksum(bytes, size)};
    {
        std::ofstream f(temp, std::ios::binary | std::ios::trunc);
        f.write(reinterpret_cast<const char*>(header.data()), 32);
        f.write(static_cast<const char*>(bytes), std::streamsize(size));
        f.close();
        if (!f) { std::filesystem::remove(temp, ec); return; }
    }
    // Readers never see a partially written entry. A competing process may win.
#ifdef _WIN32
    if (!MoveFileExW(temp.c_str(), dest.c_str(), MOVEFILE_REPLACE_EXISTING)) std::filesystem::remove(temp, ec);
#else
    std::filesystem::rename(temp, dest, ec);
    if (ec) std::filesystem::remove(temp, ec);
#endif
}
}
