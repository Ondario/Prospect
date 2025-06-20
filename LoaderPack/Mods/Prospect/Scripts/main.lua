-- local cs = FindAllOf("YControllerContractsActivesDataComponent_BP_C")
-- for _, c in ipairs(cs) do
--     if string.find(contractsActives:GetFullName(), "/Game/Maps") then
--         print(contractsActives:GetFullName())
--         local data = {
--             { contractId = "Main-KOR-GainTrust-1", objectivesProgress = { 3 } },
--             { contractId = "Main-Osiris-FTUE-1",   objectivesProgress = { 1 } },
--             { contractId = "Main-ICA-GainTrust-2", objectivesProgress = { 1, 3 }}
--         }
--         contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = data })
--     end
-- end

local http = require("socket.http")
local ltn12 = require("ltn12")
local json = require("dkjson")

---@param activeContractsProgressClamp TArray<FYContractProgress>
---@return table
function copyActiveContractsProgress(activeContractsProgressClamp)
    local data = {}
    activeContractsProgressClamp:ForEach(function (idx, elem)
        local contractData = elem:get()
        local contract = {
            contractId = contractData.contractId:ToString(),
            objectivesProgress = {}
        }
        contractData.objectivesProgress:ForEach(function (i, e)
            table.insert(contract.objectivesProgress, e:get())
        end)
        table.insert(data, contract)
    end)
    return data
end

ExecuteInGameThread(function()
    ---@class UYBackendControllerLibrary
    local GUBackendController = FindFirstOf('YBackendControllerLibrary')

    -- TODO: Probably these globals can be replaced
    -- Currently here for improved performance
    ---@class APlayerController?
    local Player = nil
    ---@class UYGameInstance
    local GameInstance = nil
    local CurrentProgressClamp = nil

    --- Always occurs before map is loaded
    RegisterHook("/Script/Engine.PlayerController:ServerAcknowledgePossession", function () end, function(self)
        print("ServerAcknowledgePossession triggered")
        Player = self:get()
        print(Player:GetFullName())

        ---@class UYPlayerInitializationComponent
        local PlayerInitializationComponent = Player.m_initializationComponent
        --- Allows the player to spawn
        PlayerInitializationComponent:NotifyClientAboutServerFinishedInitialization()
    end)

    --- Occurs when active contracts are received from the backend
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:HandlePlayerActiveContractsReceived", function () end, function(self)
        print("HandlePlayerActiveContractsReceived triggered")
        ---@class UYControllerContractsActivesDataComponent
        local contractsActives = self:get()
        CurrentProgressClamp = copyActiveContractsProgress(contractsActives.m_contractsCurrentProgressClamp.activeContractsProgressClamp)
    end)

    -- Occurs when PlayerIntroComponent:TryStartDropPodIntro() is called
    -- RegisterHook("/Game/Components/YPlayerDropPodIntroComponent_BP.YPlayerDropPodIntroComponent_BP_C:StartIntroSequence", function(self)
    --     print("YPlayerDropPodIntroComponent_BP_C:StartIntroSequence")
    -- end)

    -- Occurs at the end of the drop pod intro sequence.
    -- There also seems to be an artificial ~3 seconds delay after exiting the drop pod.
    -- RegisterHook("/Game/Components/YPlayerDropPodIntroComponent_BP.YPlayerDropPodIntroComponent_BP_C:EndIntroSequence", function(self)
    --     print("YPlayerDropPodIntroComponent_BP_C:EndIntroSequence")
    --     ---@class UYPlayerIntroComponent
    --     local PlayerIntroComponents = FindAllOf('YPlayerIntroComponent') ---@type UYPlayerIntroComponent[]
    --     for _, PlayerIntroComponent in ipairs(PlayerIntroComponents) do
    --         if string.find(PlayerIntroComponent:GetFullName(), "/Game/Maps") then
    --             --- Unblocks client movement once drop pod intro sequence is finished.
    --             PlayerIntroComponent:ServerAcknowledgeIntroFinished()
    --             break
    --         end
    --     end
    -- end)

    --- Occurs once all activity actors are available
    RegisterHook("/Script/Prospect.YActivityLocationsManager:OnActivitiesLoaded", function () end, function(self)
        print("Activities loaded")
        ---@class AAAM_Escape_BP_C
        local ActivityManagerEscapeActors = FindAllOf('AAM_Escape_BP_C') ---@type AAAM_Escape_BP_C[]
        for _, e in ipairs(ActivityManagerEscapeActors) do
            if string.find(e:GetFullName(), "/Game/Maps") then
                --- This sets the evacs for the player on server join
                e:OnPlayerJoined(Player.PlayerState);
                --- Enables all evacs
                --- e:DEBUG_EnableAllEvacLocations()
                break
            end
        end
    end)

    --- Occurs once after authorization is complete on login screen
    RegisterHook("/Script/Prospect.YGameInstance:OnAuthorizationComplete", function () end, function(self)
        GameInstance = self:get()
        print(GameInstance:GetFullName())
    end)

    -- Fires when an objective is being updated (either on contracts init or on objective progress update).
    -- NOTE: For some reason, does not fire on OnCharacterVisitedArea and TryConsumeDeadDropItems, but has the progress set.
    -- Store the current progress before execution.
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:TrackerProgressUpdate", function(self, newData)
        print("TrackerProgressUpdate")
        ---@class FYContractsProgress
        local contractsProgress = newData:get()
        CurrentProgressClamp = copyActiveContractsProgress(contractsProgress.activeContractsProgressClamp)
    end)

    -- Fires on client when the client visits areas of interest.
    -- The tracker is updated post-execution.
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:OnCharacterVisitedArea", function () end, function(self)
        print("Visited area")
        ---@class UYControllerContractsActivesDataComponent
        local contractsActives = self:get()
        local data = copyActiveContractsProgress(contractsActives.m_contractsCurrentProgressClamp.activeContractsProgressClamp)

        -- Reset old progress
        contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = CurrentProgressClamp })
        -- Write updated progress so notification appears
        contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = data })
    end)

    -- Fires on client when attempting to store dead drop items.
    -- The tracker is updated post-execution.
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:TryConsumeDeadDropItems", function () end, function(self)
        print("Consuming dead drop items")
        ---@class UYControllerContractsActivesDataComponent
        local contractsActives = self:get()
        local data = copyActiveContractsProgress(contractsActives.m_contractsCurrentProgressClamp.activeContractsProgressClamp)

        -- Reset old progress
        contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = CurrentProgressClamp })
        -- Write updated progress so notification appears
        contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = data })
    end)

    -- Occurs on end of match screen (on evac, death, or voluntary leave).
    -- Hook order does not matter since OnPlayerFinishedMatch won't do anything on the client side.
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:OnPlayerFinishedMatch", function(self)
        print("Sending contracts data...")
        ---@class UYControllerContractsActivesDataComponent
        local contractsActives = self:get()
        local data = {
            userId = GUBackendController:FindUniqueId(Player, 0):ToString(),
            contracts = {}
        }
        contractsActives.m_contractsCurrentProgressClamp.activeContractsProgressClamp:ForEach(function (idx, elem)
            local contractData = elem:get()
            local contract = {
                contractId = contractData.contractId:ToString(),
                progress = {}
            }
            contractData.objectivesProgress:ForEach(function (i, e)
                table.insert(contract.progress, e:get())
            end)
            table.insert(data.contracts, contract)
        end)
        local json_data = json.encode(data)
        local playfabRequest = {
            FunctionName = "UpdatePlayerActiveContracts",
            FunctionParameter = json_data,
            GeneratePlayStreamEvent = false
        }
        local out = json.encode(playfabRequest)
        print(out)

        local response = {}

        -- Send the request
        -- TODO: Not using UPlayFabCloudScriptAPI interface because UE4SS
        -- fails to construct FCloudScriptExecuteFunctionRequest (or I haven't figured out how to do that properly)
        -- TODO: Use LuaSec with HTTPS or figure out how to use UPlayFabCloudScriptAPI
        local _, status = http.request{
            url = "http://127.0.0.1:8000/CloudScript/ExecuteFunction",
            method = "POST",
            headers = {
                ["Host"] = "127.0.0.1:8000",
                ["Content-Type"] = "application/json",
                ["Content-Length"] = tostring(#out),
                ["X-EntityToken"] = GameInstance.m_authorizationManager.m_playfabInstance.m_authContext.m_entityToken:ToString(),
                ["Connection"] = "close"
            },
            source = ltn12.source.string(out),
            sink = ltn12.sink.table(response)
        }
        print("Status:", status)
        print("Response:")
        print(table.concat(response))
    end)
end)

-- Set mode manually here: "server" or "client"
local mode = "client"  -- Change to "client" if needed

if mode == "server" then
    print("[MainMod] Running in server mode.")
    -- Place your server-specific hooks and logic here

elseif mode == "client" then
    print("[MainMod] Running in client mode.")

    -- Block travel/session/join functions (test one at a time)
    -- RegisterHook("/Script/Prospect.YControllerTravelComponent:TryTravelToSession", function()
    --     print("[ClientMod] Blocked TryTravelToSession")
    --     return true -- block original
    -- end)

    RegisterHook("/Script/Prospect.YControllerTravelComponent:OnMatchMakingResultReceived", function()
        print("[ClientMod] Blocked OnMatchMakingResultReceived")
        return true -- block original
    end)

    RegisterHook("/Script/Prospect.YControllerTravelComponent:ClientRequestTravel", function()
        print("[ClientMod] Blocked ClientRequestTravel")
        return true -- block original
    end)

    --[[
    RegisterHook("/Script/Prospect.YControllerTravelComponent:ExecuteTravel", function()
        print("[ClientMod] Blocked ExecuteTravel")
        return true -- block original
    end)
    --]]

    -- Block matchmaking/start match functions (optional, may not be needed)
    --[[
    RegisterHook("/Script/Prospect.YMatchmakingManager:EnterPersistentMatchInternal", function()
        print("[ClientMod] Blocked EnterPersistentMatchInternal")
        return true -- block original
    end)

    RegisterHook("/Script/Prospect.YMatchmakingManager:SetReadyForMatch", function()
        print("[ClientMod] Blocked SetReadyForMatch")
        return true -- block original
    end)
    --]]

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

else
    print("[MainMod] Mode variable missing or invalid! Please set to 'server' or 'client'.")
end