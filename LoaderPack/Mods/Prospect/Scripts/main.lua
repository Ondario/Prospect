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

-- Add at the top of the file, after the requires
local function debugLog(message)
    print("[Prospect] " .. message)
end

ExecuteInGameThread(function()
    debugLog("Mod initialized!")
    
    ---@class UYBackendControllerLibrary
    local GUBackendController = FindFirstOf('YBackendControllerLibrary')
    debugLog("Backend Controller found: " .. tostring(GUBackendController ~= nil))

    -- TODO: Probably these globals can be replaced
    -- Currently here for improved performance
    ---@class APlayerController?
    local Player = nil
    ---@class UYGameInstance
    local GameInstance = nil
    local CurrentProgressClamp = nil

    -- Hook into authentication
    RegisterHook("/Script/Prospect.YGameInstance:OnAuthenticationComplete", function(self, entityToken, userId)
        debugLog("Authentication Complete:")
        debugLog("  Entity Token: " .. tostring(entityToken))
        debugLog("  User ID: " .. tostring(userId))
    end)

    -- Hook into matchmaking
    RegisterHook("/Script/Prospect.YMatchmakingManager:SetReadyForMatch", function(self, isReady, selectedMapName)
        debugLog("Matchmaking Ready:")
        debugLog("  Ready: " .. tostring(isReady))
        debugLog("  Map: " .. tostring(selectedMapName))
        debugLog("  User ID: " .. tostring(self:GetUserId()))
    end)

    -- Hook into travel request creation
    RegisterHook("/Script/Prospect.YControllerTravelComponent:CreateTravelRequest", function () end, function(self)
        debugLog("=== Travel Request ===")
        local travelData = self:get().m_travelData
        debugLog("Context: " .. travelData.m_context)
        debugLog("Map Name: " .. travelData.m_mapName)
        debugLog("Instance Type: " .. tostring(travelData.m_instanceType))
        debugLog("Load Map Directly: " .. tostring(travelData.m_loadMapDirectly))
        debugLog("Generated Request: " .. tostring(travelData.m_generatedRequest))
        debugLog("Evaluate Session State: " .. tostring(travelData.m_evaluateSessionState))
        debugLog("Cancel Existing Travel: " .. tostring(travelData.m_cancelExistingTravel))
        debugLog("Wait For Resources: " .. tostring(travelData.m_waitForResources))
    end)

    -- Hook into travel
    RegisterHook("/Script/Prospect.YControllerTravelComponent:TryTravelToSession", function(self, sessionId)
        debugLog("Travel to Session:")
        debugLog("  Session ID: " .. tostring(sessionId))
        debugLog("  User ID: " .. tostring(self:GetUserId()))
    end)

    -- Hook into session state
    RegisterHook("/Script/Prospect.YControllerTravelComponent:OnPendingSessionReturn", function(self, result)
        debugLog("Session State:")
        debugLog("  Can Go To Session: " .. tostring(result.canGoToSession))
        debugLog("  Should Cancel: " .. tostring(result.shouldCancel))
        debugLog("  Connection Data:")
        debugLog("    Address: " .. tostring(result.connectionData.addr))
        debugLog("    Session ID: " .. tostring(result.connectionData.sessionId))
        debugLog("    Server ID: " .. tostring(result.connectionData.serverId))
        debugLog("    Region: " .. tostring(result.connectionData.region))
        debugLog("    Is Match: " .. tostring(result.connectionData.m_isMatch))
    end)

    -- Hook into player spawn
    RegisterHook("/Script/Engine.PlayerController:ServerAcknowledgePossession", function () end, function(self)
        debugLog("=== Player Spawn ===")
        Player = self:get()
        debugLog("Player Name: " .. Player:GetFullName())
        debugLog("Player State: " .. Player.PlayerState:GetFullName())
        debugLog("Player Location: " .. Player:GetActorLocation():ToString())
        debugLog("Player Rotation: " .. Player:GetActorRotation():ToString())
        debugLog("Player ID: " .. Player.PlayerState:GetPlayerId():ToString())

        ---@class UYPlayerInitializationComponent
        local PlayerInitializationComponent = Player.m_initializationComponent
        --- Allows the player to spawn
        PlayerInitializationComponent:NotifyClientAboutServerFinishedInitialization()
    end)

    -- Hook into match join
    RegisterHook("/Script/Prospect.YActivityLocationsManager:OnActivitiesLoaded", function () end, function(self)
        debugLog("=== Match Join ===")
        debugLog("Activities loaded")
        ---@class AAAM_Escape_BP_C
        local ActivityManagerEscapeActors = FindAllOf('AAM_Escape_BP_C') ---@type AAAM_Escape_BP_C[]
        for _, e in ipairs(ActivityManagerEscapeActors) do
            if string.find(e:GetFullName(), "/Game/Maps") then
                debugLog("Activity Manager: " .. e:GetFullName())
                debugLog("Player State: " .. Player.PlayerState:GetFullName())
                --- This sets the evacs for the player on server join
                e:OnPlayerJoined(Player.PlayerState);
                break
            end
        end
    end)

    -- Hook into player movement
    RegisterHook("/Script/Engine.PlayerController:ClientUpdatePosition", function () end, function(self)
        debugLog("=== Player Movement ===")
        debugLog("Player Location: " .. Player:GetActorLocation():ToString())
        debugLog("Player Rotation: " .. Player:GetActorRotation():ToString())
        debugLog("Player Velocity: " .. Player:GetVelocity():ToString())
    end)

    -- Hook into player actions
    RegisterHook("/Script/Engine.PlayerController:ServerStartFire", function () end, function(self)
        debugLog("=== Player Action ===")
        debugLog("Player started firing")
        debugLog("Player Location: " .. Player:GetActorLocation():ToString())
        debugLog("Player Rotation: " .. Player:GetActorRotation():ToString())
    end)

    -- Hook into match leave
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:OnPlayerFinishedMatch", function(self)
        debugLog("=== Match Leave ===")
        debugLog("Player finished match")
        debugLog("Player State: " .. Player.PlayerState:GetFullName())
        debugLog("Player Location: " .. Player:GetActorLocation():ToString())
        debugLog("Player Rotation: " .. Player:GetActorRotation():ToString())
    end)

    -- Hook into inventory updates
    RegisterHook("/Script/Prospect.YInventoryManager:SendCompleteInventoryUpdate", function(self, userId, items)
        debugLog("Inventory Update:")
        debugLog("  User ID: " .. tostring(userId))
        for _, item in ipairs(items) do
            debugLog("  Item:")
            debugLog("    Custom ID: " .. tostring(item.m_customItemID))
            debugLog("    Type: " .. tostring(item.itemType))
            debugLog("    Rarity: " .. tostring(item.rarityType))
            debugLog("    Amount: " .. tostring(item.amount))
            debugLog("    Vanity Amount: " .. tostring(item.vanityAmount))
        end
    end)

    -- Occurs when active contracts are received from the backend
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:HandlePlayerActiveContractsReceived", function () end, function(self)
        debugLog("HandlePlayerActiveContractsReceived triggered")
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

    --- Fires when an objective is being updated (either on contracts init or on objective progress update).
    -- NOTE: For some reason, does not fire on OnCharacterVisitedArea and TryConsumeDeadDropItems, but has the progress set.
    -- Store the current progress before execution.
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:TrackerProgressUpdate", function(self, newData)
        debugLog("TrackerProgressUpdate")
        ---@class FYContractsProgress
        local contractsProgress = newData:get()
        CurrentProgressClamp = copyActiveContractsProgress(contractsProgress.activeContractsProgressClamp)
    end)

    -- Fires on client when the client visits areas of interest.
    -- The tracker is updated post-execution.
    RegisterHook("/Script/Prospect.YControllerContractsActivesDataComponent:OnCharacterVisitedArea", function () end, function(self)
        debugLog("Visited area")
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
        debugLog("Consuming dead drop items")
        ---@class UYControllerContractsActivesDataComponent
        local contractsActives = self:get()
        local data = copyActiveContractsProgress(contractsActives.m_contractsCurrentProgressClamp.activeContractsProgressClamp)

        -- Reset old progress
        contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = CurrentProgressClamp })
        -- Write updated progress so notification appears
        contractsActives:TrackerProgressUpdate({ activeContractsProgressClamp = data })
    end)

    -- Hook into squad matchmaking
    RegisterHook("/Script/Prospect.YMatchmakingManager:OnMatchmakingResultReceived", function(self, result)
        debugLog("Matchmaking Result:")
        debugLog("  Success: " .. tostring(result.success))
        debugLog("  Blocker: " .. tostring(result.blocker))
        debugLog("  Attempts: " .. tostring(result.numAttempts))
        debugLog("  Session ID: " .. tostring(result.sessionId))
        debugLog("  Is Match Travel: " .. tostring(result.isMatchTravel))
    end)

    -- Hook into player replication
    RegisterHook("/Script/Prospect.YPlayerCharacter:OnRep_PlayerState", function(self)
        debugLog("Player State Updated:")
        debugLog("  Player ID: " .. tostring(self:GetPlayerId()))
        debugLog("  Is Spawned: " .. tostring(self:IsSpawned()))
    end)

    -- Hook into network connection state
    RegisterHook("/Script/Prospect.YNetworkConnectionComponent:OnConnectionStateChanged", function () end, function(self)
        debugLog("=== Network Connection ===")
        debugLog("Connection state changed")
        local state = self:get().m_connectionState
        debugLog("Connection State: " .. tostring(state))
    end)

    -- Hook into player synchronization
    RegisterHook("/Script/Prospect.YPlayerSynchronizationComponent:OnPlayerSynchronized", function () end, function(self)
        debugLog("=== Player Synchronization ===")
        debugLog("Player synchronized")
        local playerId = self:get().m_playerId
        debugLog("Player ID: " .. playerId)
    end)

    -- Hook into match state changes
    RegisterHook("/Script/Prospect.YGameModeBase:OnMatchStateChanged", function () end, function(self)
        debugLog("=== Match State ===")
        debugLog("Match state changed")
        local matchState = self:get().m_matchState
        debugLog("Match State: " .. tostring(matchState))
    end)

    -- Hook into world composition
    RegisterHook("/Script/Prospect.YGameInstance:OnWorldCompositionLoaded", function(self, worldComposition)
        debugLog("World Composition Loaded:")
        for _, level in ipairs(worldComposition.levels) do
            debugLog("  Level: " .. tostring(level.name))
        end
    end)

    debugLog("Prospect mod loaded successfully!")
end)