#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <MinHook.h>
#include <cstdio>
#include "logger.h"
#include "SDK.h"
#include "SDK_Memory.h"
#include <string>
#include <fstream>
#include <sstream>
#include <algorithm>
#include <cctype>
#include <locale>

std::wstring g_backendUrl;

// trim from start (in place)
inline void ltrim(std::wstring &s) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](unsigned char ch) {
        return !std::isspace(ch);
    }));
}

// trim from end (in place)
inline void rtrim(std::wstring &s) {
    s.erase(std::find_if(s.rbegin(), s.rend(), [](unsigned char ch) {
        return !std::isspace(ch);
    }).base(), s.end());
}

inline void trim(std::wstring &s) {
    rtrim(s);
    ltrim(s);
}

constexpr uintptr_t OffsetGMalloc = 0x63EC4A0;

/**
 * Function offset, find with "?sdk=".
 */
constexpr uintptr_t OffsetPlayFabApiGetUrl = 0xCEEF00;

/**
 * Function hooks.
 */
typedef SDK::FString(__fastcall* tPlayFabApiGetUrl)(SDK::UPlayFabAPISettings*, const SDK::FString&);

tPlayFabApiGetUrl OldPlayFabApiGetUrl;

SDK::FString __fastcall PlayFabApiGetUrlProxy(SDK::UPlayFabAPISettings* thiz, const SDK::FString& callPath) {
    logger::Print("[Agent] GetURL called with callPath %s\n", callPath.ToString().c_str());

    if (!g_backendUrl.empty())
    {
        return g_backendUrl + std::wstring(callPath.c_str());
    }

    return std::wstring(L"https://127.0.0.1:8443") + std::wstring(callPath.c_str());
}

constexpr uintptr_t OffsetSetReadyForMatch = 0x19675D0;

typedef void(__fastcall* tSetReadyForMatch)(void* pThis /*, add params if you know them */);
tSetReadyForMatch OldSetReadyForMatch = nullptr;

void __fastcall SetReadyForMatch_Hook(void* pThis /*, add params if needed */) {
    logger::Print("[Agent] Blocked SetReadyForMatch!\n");
    // Do NOT call the original to block matchmaking
    // OldSetReadyForMatch(pThis, ...);
}

bool ShouldBlockSetReadyForMatch() {
    std::ifstream config("Server.config");
    if (!config.is_open()) return true; // Default to blocking if file missing

    std::string line;
    while (std::getline(config, line)) {
        if (line.find("block_set_ready_for_match=") == 0) {
            char value = line.back();
            return value == '1';
        }
    }
    return true; // Default to blocking if not found
}

DWORD WINAPI OnDllAttach(LPVOID base) {
	logger::Print("[Agent] DLL Attached\n");

	const auto hModule = GetModuleHandleW(nullptr);
	const auto hModulePtr = reinterpret_cast<uintptr_t>(hModule);

	// Initialize GMalloc.
	SDK::GMalloc = reinterpret_cast<SDK::FMalloc**>(hModulePtr + OffsetGMalloc);

	// Initialize MinHook.
	if (MH_Initialize() != MH_OK) {
		logger::Print("[Agent] Failed to initialize MinHook\n");
		FreeLibraryAndExitThread(hModule, 1);
	}

	// Hook UPlayFabAPISettings::GetUrl.
	const auto pTarget = reinterpret_cast<LPVOID>(hModulePtr + OffsetPlayFabApiGetUrl);
	const auto ppOriginal = reinterpret_cast<LPVOID*>(&OldPlayFabApiGetUrl);
	const auto mhStatus = MH_CreateHook(pTarget, &PlayFabApiGetUrlProxy, ppOriginal);

	if (mhStatus != MH_OK) {
		logger::Print("[Agent] Failed to create PlayFabAPIGetUrl hook (%d)\n", mhStatus);
		FreeLibraryAndExitThread(hModule, 1);
	}

	if (MH_EnableHook(pTarget) != MH_OK) {
		logger::Print("[Agent] Failed to enable PlayFabAPIGetUrl hook\n");
		FreeLibraryAndExitThread(hModule, 1);
	}

	if (ShouldBlockSetReadyForMatch()) {
		// Hook UYMatchmakingManager::SetReadyForMatch
		const auto pTargetSetReadyForMatch = reinterpret_cast<LPVOID>(hModulePtr + OffsetSetReadyForMatch);
		const auto ppOriginalSetReadyForMatch = reinterpret_cast<LPVOID*>(&OldSetReadyForMatch);
		const auto mhStatusSetReadyForMatch = MH_CreateHook(pTargetSetReadyForMatch, &SetReadyForMatch_Hook, ppOriginalSetReadyForMatch);

		if (mhStatusSetReadyForMatch != MH_OK) {
			logger::Print("[Agent] Failed to create SetReadyForMatch hook (%d)\n", mhStatusSetReadyForMatch);
			FreeLibraryAndExitThread(hModule, 1);
		}

		if (MH_EnableHook(pTargetSetReadyForMatch) != MH_OK) {
			logger::Print("[Agent] Failed to enable SetReadyForMatch hook\n");
			FreeLibraryAndExitThread(hModule, 1);
		}
		logger::Print("[Agent] SetReadyForMatch hook ENABLED by config.\n");
	} else {
		logger::Print("[Agent] SetReadyForMatch hook DISABLED by config.\n");
	}

	const wchar_t* filePath = L"backend.txt";
	std::wifstream f(filePath);
	if (f) {
		std::wstringstream buffer;
		buffer << f.rdbuf();
		g_backendUrl = buffer.str();
		trim(g_backendUrl);
		if (g_backendUrl.rfind(L"https://", 0) != 0) {
			logger::Print("[Agent] URL must start with https://\n");
			g_backendUrl.clear();
		} else {
			logger::Print("[Agent] Loaded custom URL: %S\n", g_backendUrl.c_str());
		}
	} else {
		logger::Print("[Agent] Failed to open file: %S\n", filePath);
	}

	logger::Print("[Agent] DLL Initialized\n");
	ExitThread(1);
}

BOOL WINAPI DllMain(
    const _In_      HINSTANCE hinstDll,
    const _In_      DWORD     fdwReason,
    const _In_opt_  LPVOID    lpvReserved
) {
	if (fdwReason == DLL_PROCESS_ATTACH) {
		DisableThreadLibraryCalls(hinstDll);

		// Allocate a console.
		AllocConsole();
		freopen_s(reinterpret_cast<FILE**>(stdout), "CONOUT$", "w", stdout);

		// Attach logger.
		logger::Attach();

		// Spawn thread.
		CreateThread(nullptr, 0, OnDllAttach, hinstDll, 0, nullptr);
	} else if (fdwReason == DLL_PROCESS_DETACH) {
		// Detach logger.
		logger::Detach();
	}

    return TRUE;
}
