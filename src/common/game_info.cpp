#include "app/app.h"
bool AzurikKeyboardMessage(void*, uint32_t, uintptr_t, intptr_t, intptr_t*);
void AzurikKeyboardPad(int, input::Pad&);
void AzurikRumble(int, uint16_t&, uint16_t&);

// what the runtime asks of the game; strings must be set, hooks may stay null
const app::GameInfo app::kGame = {
    .title = L"Azurik - Rise of Perathia",               // window and splash
    .short_title = L"Azurik",               // message boxes
    .name = L"Azurik",                      // window classes, crash dumps, handover event
    .log = "azurik",                        // log file prefix
    .settings = "settings.toml",          // settings file beside the exe
    .splash = L"splash",                  // optional splash.png or splash.bmp beside the exe
    .missing = L"Put the game in the assets folder: its disc image (.iso), or default.xbe with its folders beside it.",
    .images = {{"default.xbe", "Azurik"}},  // each recompiled xbe and its exe, for title launches
    .options = {},                        // game settings kept in the settings file
    .load = nullptr,                      // reads more from the settings file's toml::table
    .save = nullptr,                      // writes more into it
    .init = nullptr,                      // after the settings load, before the window and boot
    .loaded = nullptr,                    // image in memory, before its entry point runs
    .window = nullptr,                    // the game window is up
    .message = AzurikKeyboardMessage,
    .ui = nullptr,                        // ImGui drawing each frame
    .pad = AzurikKeyboardPad,
    .rumble = AzurikRumble,                    // changes a rumble before it is set
};
