---@meta

---@class UWBP_SocialToggle_C : UYWidget_OutpostSocialToggle
---@field UberGraphFrame FPointerToUberGraphFrame
---@field HighlightBracket_Anim UWidgetAnimation
---@field notificationNumber UNotificationNumber_WBP_C
---@field onlineNumberYTextBlock UYTextBlock
---@field socialYToggleButton UYButton
---@field WBP_Notification UWBP_Notification_C
---@field WBP_SelectionBracket UWBP_SelectionBracket_C
---@field OnSocialToggleClicked FWBP_SocialToggle_COnSocialToggleClicked
local UWBP_SocialToggle_C = {}

---@return UWidget
function UWBP_SocialToggle_C:Get_SocialButton_ToolTipWidget() end
function UWBP_SocialToggle_C:BndEvt__SquadToggleButton_K2Node_ComponentBoundEvent_1_OnButtonHoverEvent__DelegateSignature() end
function UWBP_SocialToggle_C:BndEvt__SquadToggleButton_K2Node_ComponentBoundEvent_2_OnButtonClickedEvent__DelegateSignature() end
---@param numOfOnlineFriends int32
function UWBP_SocialToggle_C:BP_SetNumberOfOnlineFriends(numOfOnlineFriends) end
function UWBP_SocialToggle_C:BndEvt__SquadToggleButton_K2Node_ComponentBoundEvent_0_OnButtonHoverEvent__DelegateSignature() end
---@param numOfInvites int32
function UWBP_SocialToggle_C:BP_SetNumberOfInvites(numOfInvites) end
---@param EntryPoint int32
function UWBP_SocialToggle_C:ExecuteUbergraph_WBP_SocialToggle(EntryPoint) end
function UWBP_SocialToggle_C:OnSocialToggleClicked__DelegateSignature() end


