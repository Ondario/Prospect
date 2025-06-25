---@meta

---@class UWBP_SquadOverview_Entry_C : UYWidget_OutpostSquadOverviewEntry
---@field UberGraphFrame FPointerToUberGraphFrame
---@field HighlightBracket_Anim UWidgetAnimation
---@field addMemberImage UImage
---@field EntryOverlay UOverlay
---@field Gfx_PlayerBackground UImage
---@field Gfx_PlayerBackgroundStroke UImage
---@field Gfx_StatusNotReady UImage
---@field Gfx_StatusNotReadyBacker UImage
---@field Gfx_StatusReady UImage
---@field Gfx_StatusReadyBacker UImage
---@field Gfx_StatusReadyUnder UImage
---@field Icn_Avatar UImage
---@field Image UUI_ImageBase_WBP_C
---@field ReadyStatus UOverlay
---@field StatusSwitcher UWidgetSwitcher
---@field talkingIcon_Image UWBP_SquadTalking_C
---@field WBP_SelectionBracket UWBP_SelectionBracket_C
---@field YWidget_ProspectorLevel_Small_WBP UYWidget_ProspectorLevel_Small_WBP_C
---@field Add_ToolTipWidget UWBP_Generic_ToolTip_C
---@field Player_ToolTipWidget UWBP_Generic_ToolTip_C
---@field ['Player Name'] FText
---@field IsOwnPlayer boolean
local UWBP_SquadOverview_Entry_C = {}

function UWBP_SquadOverview_Entry_C:SetReadyStateVisibility() end
function UWBP_SquadOverview_Entry_C:OnMatchmakingSettingsUpdated() end
---@return UWidget
function UWBP_SquadOverview_Entry_C:Get_ToolTipWidget_Player() end
---@return UWidget
function UWBP_SquadOverview_Entry_C:Get_ToolTipWidget_Add() end
---@param playerName FText
function UWBP_SquadOverview_Entry_C:BP_NotifyDataSetup(playerName) end
function UWBP_SquadOverview_Entry_C:BndEvt__m_addMemberButton_K2Node_ComponentBoundEvent_0_OnButtonPressedEvent__DelegateSignature() end
function UWBP_SquadOverview_Entry_C:BndEvt__m_addMemberButton_K2Node_ComponentBoundEvent_1_OnButtonReleasedEvent__DelegateSignature() end
---@param IsDesignTime boolean
function UWBP_SquadOverview_Entry_C:PreConstruct(IsDesignTime) end
---@param PlayerId FString
function UWBP_SquadOverview_Entry_C:BP_NotifyPlayerBound(PlayerId) end
function UWBP_SquadOverview_Entry_C:BndEvt__m_addMemberButton_K2Node_ComponentBoundEvent_63_OnButtonHoverEvent__DelegateSignature() end
function UWBP_SquadOverview_Entry_C:BndEvt__m_addMemberButton_K2Node_ComponentBoundEvent_50_OnButtonHoverEvent__DelegateSignature() end
function UWBP_SquadOverview_Entry_C:BndEvt__m_squadMemberButton_K2Node_ComponentBoundEvent_26_OnButtonHoverEvent__DelegateSignature() end
function UWBP_SquadOverview_Entry_C:Construct() end
function UWBP_SquadOverview_Entry_C:BndEvt__m_squadMemberButton_K2Node_ComponentBoundEvent_13_OnButtonHoverEvent__DelegateSignature() end
---@param EntryPoint int32
function UWBP_SquadOverview_Entry_C:ExecuteUbergraph_WBP_SquadOverview_Entry(EntryPoint) end


