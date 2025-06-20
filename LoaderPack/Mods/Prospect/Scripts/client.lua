-- client.lua
print("[ClientMod] Client mod loaded and running.")

-- Block matchmaking/start match functions
RegisterHook("/Script/Prospect.YMatchmakingManager:EnterPersistentMatchInternal", function()
    print("[ClientMod] Blocked EnterPersistentMatchInternal")
    return true -- block original
end)

RegisterHook("/Script/Prospect.YMatchmakingManager:SetReadyForMatch", function()
    print("[ClientMod] Blocked SetReadyForMatch")
    return true -- block original
end)

-- Add keybind to join host
RegisterKeyBindAsync(Key.F5, {}, function()
    local ip_file = io.open("backend.txt", "r")
    local ip = ip_file and ip_file:read("*l") or ""
    if ip_file then ip_file:close() end

    if ip == "" then
        print("[ClientMod] backend.txt is missing or empty!")
        return
    end

    print("[ClientMod] Sending join to:", ip)
    local playerController = FindFirstOf("PlayerController")
    if playerController then
        playerController:ConsoleCommand("open " .. ip)
    else
        print("[ClientMod] PlayerController not found!")
    end
end) 