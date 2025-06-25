#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <stdint.h>
#include <string>

std::wstring ExePath() {
    TCHAR buffer[MAX_PATH] = { 0 };
    GetModuleFileName( NULL, buffer, MAX_PATH );
    std::wstring::size_type pos = std::wstring(buffer).find_last_of(L"\\/");
    return std::wstring(buffer).substr(0, pos);
}

// Function to create and launch a process with parameters
HANDLE StartProcess(const wchar_t* exePath, DWORD* processID) {
    STARTUPINFO si = { 0 };
    PROCESS_INFORMATION pi = { 0 };
    si.cb = sizeof(si);

    // Command-line arguments to pass to the process
    wchar_t commandLine[MAX_PATH] = L""; // Buffer for full command
    _snwprintf_s(commandLine, sizeof(commandLine) / sizeof(wchar_t), L"\"%s\" -log -server -steam_auth PF_TITLEID=2EA46", exePath);

    if (CreateProcess(NULL, commandLine, NULL, NULL, FALSE, CREATE_SUSPENDED, NULL, NULL, &si, &pi)) {
        *processID = pi.dwProcessId;
        printf("Successfully started process: %ls (PID: %lu)\n", exePath, *processID);
        ResumeThread(pi.hThread);
        CloseHandle(pi.hThread);
        return pi.hProcess;
    }
    else {
        wchar_t szErrorMessage[512];

        DWORD dwErrorCode = GetLastError();
        FormatMessage(
            FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
            NULL,
            dwErrorCode,
            0,
            szErrorMessage,
            sizeof(szErrorMessage),
            NULL
        );

        printf("Failed to start process. Error: %lu: %ws\n", dwErrorCode, szErrorMessage);
        return NULL;
    }
}

// Function to get the base address of the main module (PE Image)
uintptr_t GetModuleBaseAddress(DWORD processID) {
    uintptr_t baseAddress = 0;
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processID);
    if (hSnapshot != INVALID_HANDLE_VALUE) {
        MODULEENTRY32 moduleEntry;
        moduleEntry.dwSize = sizeof(MODULEENTRY32);
        if (Module32First(hSnapshot, &moduleEntry)) {
            baseAddress = (uintptr_t)moduleEntry.modBaseAddr;
        }
        CloseHandle(hSnapshot);
    }
    else {
        printf("Failed to take process snapshot. Error: %lu\n", GetLastError());
    }
    return baseAddress;
}

// Function to get a handle to the target process
HANDLE OpenTargetProcess(DWORD processID) {
    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
    if (hProcess == NULL) {
        printf("Failed to open process. Error: %lu\n", GetLastError());
    }
    return hProcess;
}

bool WriteBytes(HANDLE hProcess, uintptr_t address, BYTE* newValues, size_t size) {
    SIZE_T bytesWritten;
    DWORD oldProtect;
    if (VirtualProtectEx(hProcess, (LPVOID)address, size, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        if (WriteProcessMemory(hProcess, (LPVOID)address, newValues, size, &bytesWritten)) {
            printf("Memory Write Success at address 0x%p\n", (void*)address);
        }
        else {
            printf("Failed to write memory. Error: %lu\n", GetLastError());
            return false;
        }
        // Restore original protection
        VirtualProtectEx(hProcess, (LPVOID)address, size, oldProtect, &oldProtect);
    }
    else {
        printf("Failed to change memory protection. Error: %lu\n", GetLastError());
        return false;
    }
    return true;
}

bool CheckForceLevelStreamingOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[2];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0x32 && buffer[1] == 0xDB;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckForceLevelStreamingOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool CheckGPStringOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[2];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0x5F && buffer[1] == 0x00;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckForceLevelStreamingOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool CheckWeakPointerCheckJumpOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[2];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0xF8 && buffer[1] == 0xFE;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckWeakPointerCheckJumpOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool CheckWeakPointerCheckOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[3];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0x0F && buffer[1] == 0x1F && buffer[2] == 0x40;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckWeakPointerCheckOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool PatchForceLevelStreaming(HANDLE hProcess, uintptr_t imageBase) {
    // Season 3 Patch force disable level streaming: sub_3DA7520 -> loc_3DA75B6
    uintptr_t address = imageBase + 0x3DA75B6;
    bool found = CheckForceLevelStreamingOffset(hProcess, address);
    if (!found) {
        // Season 2 Patch force disable level streaming: sub_3D19900 -> loc_3D19996
        address = imageBase + 0x3D19996;
        found = CheckForceLevelStreamingOffset(hProcess, address);
    }

    if (!found) {
        printf("Failed to patch force level streaming!\n");
        return false;
    }

    {
        BYTE newValues[2] = { 0xB3, 0x01 };
        bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
        if (!result) {
            return false;
        }
    }

    address = imageBase + 0x5541C08;
    found = CheckGPStringOffset(hProcess, address);
    if (!found) {
        address = imageBase + 0x54E0558;
        found = CheckGPStringOffset(hProcess, address);
    }

    if (!found) {
        printf("Failed to find _GP string!\n");
        return false;
    }

    {
        BYTE newValues[6] = { 0x47, 0x00, 0x00, 0x00, 0x00, 0x00 };
        bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
        if (!result) {
            return false;
        }
    }

    address = imageBase + 0x16D7C24;
    found = CheckWeakPointerCheckOffset(hProcess, address);
    // TODO
    //if (!found) {
    //    address = imageBase + 0x54E0558;
    //    found = CheckWeakPointerCheckOffset(hProcess, address);
    //}

    if (!found) {
        printf("Failed to find Weak Pointer Check!\n");
        return false;
    }

    {
        BYTE newValues[28] = {
            0x48, 0x8B, 0x06,
            0x48, 0x8D, 0x4D, 0x7F,
            0x48, 0x89, 0x45, 0x7F,
            0xE8, 0xDC, 0xC7, 0xA7, 0x00,
            0x48, 0x85, 0xC0,
            0x0F, 0x84, 0xEE, 0x00, 0x00, 0x00,
            0x90, 0x90, 0x90
        };
        bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
        if (!result) {
            return false;
        }
    }

    address = imageBase + 0x16D7D34;
    found = CheckWeakPointerCheckJumpOffset(hProcess, address);
    // TODO
    //if (!found) {
    //    address = imageBase + 0x54E0558;
    //    found = CheckWeakPointerCheckJumpOffset(hProcess, address);
    //}

    if (!found) {
        printf("Failed to find Weak Pointer Check Jump!\n");
        return false;
    }

    {
        BYTE newValues[1] = { 0xEC };
        bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
        if (!result) {
            return false;
        }
    }

    return true;
}

bool CheckUpdateInventoryFunctionOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[2];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0x84 && buffer[1] == 0xC0;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckUpdateInventoryFunctionOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool PatchUpdateInventory(HANDLE hProcess, uintptr_t imageBase) {
    uintptr_t address = imageBase + 0x18D5A11;
    bool found = CheckUpdateInventoryFunctionOffset(hProcess, address);
    if (!found) {
        address = imageBase + 0x3D19B19;
        found = CheckUpdateInventoryFunctionOffset(hProcess, address);
    }

    if (!found) {
        printf("Failed to patch inventory update function!\n");
        return false;
    }

    BYTE newValues[8] = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
    bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
    if (!result) {
        return false;
    }
    return true;
}

bool CheckSetupPlayerContractsOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[2];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0x48 && buffer[1] == 0x8D;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckSetupPlayerContractsOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool CheckUpdatePlayerContractsOffset(HANDLE hProcess, uintptr_t address) {
    BYTE buffer[2];
    SIZE_T bytesRead;
    if (ReadProcessMemory(hProcess, (LPCVOID)address, buffer, sizeof(buffer), &bytesRead)) {
        return buffer[0] == 0x3B && buffer[1] == 0xC1;
    }
    else {
        DWORD error = GetLastError();
        if (error == 299) {
            printf("Incomplete memory read. Retrying...\n");
            Sleep(1000);
            return CheckUpdatePlayerContractsOffset(hProcess, address);
        }
        printf("Failed to read memory. Error: %lu\n", error);
        return false;
    }
}

bool PatchLoadPlayerContracts(HANDLE hProcess, uintptr_t imageBase) {
    uintptr_t address = imageBase + 0x171E9EC;
    bool found = CheckUpdatePlayerContractsOffset(hProcess, address);
    if (!found) {
        printf("Failed to update player contracts function!\n");
        return false;
    }

    {
        BYTE newValues[4] = { 0x90, 0x90, 0xEB, 0x21 };
        bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
        if (!result) {
            return false;
        }
    }

    address = imageBase + 0x1706127;
    found = CheckSetupPlayerContractsOffset(hProcess, address);
    if (!found) {
        printf("Failed to find setup player contracts function!\n");
        return false;
    }

    {
        BYTE newValues[39] = {
            0x45, 0x33, 0xC0, 0x49, 0x8B, 0xD5, 0x48, 0x8B, 0x89, 0xD8, 0x00, 0x00, 0x00, 0xE8, 0x47,
            0xE2, 0x10, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
            0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xEB
        };
        bool result = WriteBytes(hProcess, address, newValues, sizeof(newValues));
        if (!result) {
            return false;
        }
    }

    return true;
}

bool Inject(HANDLE hProcess, const wchar_t* dllPath) {
    // Allocate space in the target process for the DLL path
    size_t pathSize = (wcslen(dllPath) + 1) * sizeof(wchar_t);
    LPVOID remotePath = VirtualAllocEx(hProcess, NULL, pathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (!remotePath) {
        wprintf(L"Failed to allocate memory in remote process. Error: %lu\n", GetLastError());
        CloseHandle(hProcess);
        return false;
    }

    // Write the DLL path into the target process
    if (!WriteProcessMemory(hProcess, remotePath, dllPath, pathSize, NULL)) {
        wprintf(L"Failed to write memory. Error: %lu\n", GetLastError());
        VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // Get the address of LoadLibraryW in kernel32.dll
    LPVOID loadLibraryAddr = (LPVOID)GetProcAddress(GetModuleHandleW(L"kernel32.dll"), "LoadLibraryW");
    if (!loadLibraryAddr) {
        wprintf(L"Failed to get LoadLibraryW address. Error: %lu\n", GetLastError());
        VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // Create a remote thread to call LoadLibraryW with our DLL path
    HANDLE hThread = CreateRemoteThread(
        hProcess, NULL, 0,
        (LPTHREAD_START_ROUTINE)loadLibraryAddr,
        remotePath, 0, NULL);

    if (!hThread) {
        wprintf(L"Failed to create remote thread. Error: %lu\n", GetLastError());
        VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }
    return true;
}

bool Patch(const wchar_t* processName) {
    DWORD processID;

    HANDLE hProcess = StartProcess(processName, &processID);
    if (hProcess == NULL) {
        return false;
    }
    if (!Inject(hProcess, L"Prospect.Agent.dll")) {
        return false;
    }
    if (!Inject(hProcess, L"UE4SS.dll")) {
        return false;
    }

    uintptr_t imageBase;
    // Wait a bit for PE image to load
    Sleep(250);
    while ((imageBase = GetModuleBaseAddress(processID)) == 0) {
        printf("Failed to find PE Image Base Address. Retrying...\n");
        Sleep(250);
    }
    printf("PE Image Base Address: 0x%p\n", (void*)imageBase);

    bool success = !PatchForceLevelStreaming(hProcess, imageBase) || !PatchUpdateInventory(hProcess, imageBase) || !PatchLoadPlayerContracts(hProcess, imageBase);
    if (success) {
        return false;
    }

    // Clean up
    CloseHandle(hProcess);
    return true;
}

bool WriteSteamAppIDFile() {
    const char* filename = "steam_appid.txt";
    FILE* fp;

    if (fopen_s(&fp, filename, "r") == 0) {
        // File exists, close the file
        fclose(fp);
        printf("File '%s' already exists.\n", filename);
        return true;
    }
    else {
        // File does not exist, create and write to it
        if (fopen_s(&fp, filename, "w") == 0) {
            const char* content = "480";
            size_t written = fwrite(content, sizeof(char), strlen(content), fp);
            fclose(fp);
            printf("File '%s' was created and written successfully.\n", filename);
        }
        else {
            printf("Error creating the file '%s'.\n", filename);
            return false;
        }
    }
    return true;
}

int main() {
    printf("Current working directory: %ws\n", ExePath().c_str());

    bool success = WriteSteamAppIDFile();
    if (!success) {
        // Wait for user input to exit
        printf("An error occurred when writing steam_appid.txt! Press any key to exit...\n");
        getchar();
        return 1;
    }

    success = Patch(L"Prospect-Win64-Shipping.exe");
    if (!success) {
        // Wait for user input to exit
        printf("An error occurred when patching the executable! Press any key to exit...\n");
        getchar();
        return 1;
    }

    return 0;
}