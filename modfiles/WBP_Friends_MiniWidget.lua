---@meta

---@class UWBP_Friends_MiniWidget_C : UYWidget
---@field UberGraphFrame FPointerToUberGraphFrame
---@field SquadActive_Anim UWidgetAnimation
---@field FadeIn UWidgetAnimation
---@field Gfx_Background UImage
---@field OutpostSocialToggle_WBP UWBP_SocialToggle_C
---@field OutpostSquadToggle_WBP UWBP_SquadOverview_C
---@field squad UOverlay
---@field SquadBacker UOverlay
---@field WBP_Dummy_Btn UWBP_Dummy_Btn_C
---@field WBP_InputKeyNavigation_Btn UWBP_InputKeyNavigation_Btn_C
---@field WBP_SquadStatus UWBP_SquadStatus_C
---@field headline FText
---@field WidgetClicked FWBP_Friends_MiniWidget_CWidgetClicked
local UWBP_Friends_MiniWidget_C = {}

function UWBP_Friends_MiniWidget_C:UpdateSquadStatus() end
function UWBP_Friends_MiniWidget_C:OnMatchmakingSettingsUpdated() end
function UWBP_Friends_MiniWidget_C:OnSquadInfoUpdated() end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Friends_MiniWidget_C:BndEvt__WBP_Dummy_Btn_K2Node_ComponentBoundEvent_1_OnClicked__DelegateSignature(Button) end
function UWBP_Friends_MiniWidget_C:BndEvt__OutpostSocialToggle_WBP_K2Node_ComponentBoundEvent_0_OnSocialToggleClicked__DelegateSignature() end
function UWBP_Friends_MiniWidget_C:BndEvt__OutpostSquadToggle_WBP_K2Node_ComponentBoundEvent_2_OnSquadToggleClicked__DelegateSignature() end
function UWBP_Friends_MiniWidget_C:Construct() end
---@param squadMemberInfos TArray<FYOutpostFriendInfo>
function UWBP_Friends_MiniWidget_C:OnSquadInfoUpdatedEvent(squadMemberInfos) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Friends_MiniWidget_C:BndEvt__WBP_Friends_MiniWidget_WBP_InputKeyNavigation_Btn_K2Node_ComponentBoundEvent_3_OnClicked__DelegateSignature(Button) end
---@param EntryPoint int32
function UWBP_Friends_MiniWidget_C:ExecuteUbergraph_WBP_Friends_MiniWidget(EntryPoint) end
function UWBP_Friends_MiniWidget_C:WidgetClicked__DelegateSignature() end


