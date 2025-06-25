---@meta

---@class UWBP_SocialUserList_C : UYWidget_SocialUserList
---@field UberGraphFrame FPointerToUberGraphFrame
---@field entriesVerticalBox UVerticalBox
---@field WBP_Collapsable UWBP_Collapsable_C
---@field WidgetPoolManager UYWidgetPoolManager
local UWBP_SocialUserList_C = {}

function UWBP_SocialUserList_C:ResetEntriesVisibility() end
---@param userIds TArray<FString>
function UWBP_SocialUserList_C:SetEntriesVisibilityByUserId(userIds) end
---@param socialUserEntryWBP UWBP_Social_User_Entry_C
function UWBP_SocialUserList_C:SetEntryActionButtons(socialUserEntryWBP) end
---@param friendInfo FYOutpostFriendInfo
---@return UYWidget_SocialUserEntry
function UWBP_SocialUserList_C:BP_CreateAndAddEntry(friendInfo) end
---@param IsDesignTime boolean
function UWBP_SocialUserList_C:PreConstruct(IsDesignTime) end
function UWBP_SocialUserList_C:Construct() end
---@param numOfVisibleEntries int32
function UWBP_SocialUserList_C:BP_SetNumberOfVisibleEntries(numOfVisibleEntries) end
---@param EntryPoint int32
function UWBP_SocialUserList_C:ExecuteUbergraph_WBP_SocialUserList(EntryPoint) end


