---@meta

---@class UWBP_Social_User_Entry_C : UYWidget_SocialUserEntry
---@field UberGraphFrame FPointerToUberGraphFrame
---@field WidgetOut_Anim UWidgetAnimation
---@field Gfx_Backer UImage
---@field Gfx_Gradient UImage
---@field Gfx_OnlineStatus UImage
---@field Icn_ChatMute UImage
---@field Icn_Friend UImage
---@field Icn_Profile UImage
---@field Icn_SquadState UImage
---@field Icn_VoiceMute UImage
---@field OnlineStatus UHorizontalBox
---@field SquadOverlay UOverlay
---@field SquadStatus UHorizontalBox
---@field Txt_OnlineStatus UTextBlock
---@field Txt_SeparatorState UTextBlock
---@field Txt_SquadMembers UTextBlock
---@field Txt_UserName UTextBlock
---@field Txt_UserState UTextBlock
---@field WBP_Accept_Btn UWBP_Common_Simple_Btn_C
---@field WBP_ChatMute_Btn UWBP_Common_Empty_Btn_C
---@field WBP_Decline_Btn UWBP_Common_Simple_Btn_C
---@field WBP_Report_Btn UWBP_Common_Simple_Btn_C
---@field WBP_Social_Btn UWBP_Common_Simple_Btn_C
---@field WBP_Squad_Btn UWBP_Common_Simple_Btn_C
---@field WBP_SquadLeave_Btn UWBP_Common_Simple_Btn_C
---@field WBP_VoiceMute_Btn UWBP_Common_Simple_Btn_C
---@field HideOnlineStatus boolean
---@field HideSquadStatus boolean
---@field CanShowInviteButtons boolean
---@field CanShowSocialButton boolean
---@field CanShowSquadButton boolean
---@field CanShowChatMuteButton boolean
---@field CanShowVoiceMuteButton boolean
---@field CanShowReportButton boolean
---@field CanShowBlockButton boolean
---@field DebugShowInviteButtons boolean
---@field DebugShowSocialButton boolean
---@field DebugShowSquadButton boolean
---@field DebugShowChatMuteButton boolean
---@field DebugShowVoiceMuteButton boolean
---@field DebugShowReportButton boolean
---@field DebugShowBlockButton boolean
---@field CanShowOnlineStatusButton boolean
local UWBP_Social_User_Entry_C = {}

---@param squadMemberInfos TArray<FYOutpostFriendInfo>
function UWBP_Social_User_Entry_C:OnSquadInfoUpdated(squadMemberInfos) end
---@param successful boolean
---@param response FYFriendActionResponse
function UWBP_Social_User_Entry_C:OnAddFriendResponse(successful, response) end
function UWBP_Social_User_Entry_C:SetUserData() end
function UWBP_Social_User_Entry_C:SetTooltips() end
---@param IsDesignTime boolean
function UWBP_Social_User_Entry_C:SetActionButtonReport(IsDesignTime) end
---@param IsDesignTime boolean
---@param UserId FString
function UWBP_Social_User_Entry_C:SetActionButtonVoiceMute(IsDesignTime, UserId) end
---@param IsDesignTime boolean
---@param UserId FString
function UWBP_Social_User_Entry_C:SetActionButtonChatMute(IsDesignTime, UserId) end
---@param IsDesignTime boolean
---@param UserId FString
function UWBP_Social_User_Entry_C:SetActionButtonSquad(IsDesignTime, UserId) end
---@param IsDesignTime boolean
---@param UserId FString
function UWBP_Social_User_Entry_C:SetActionButtonSocial(IsDesignTime, UserId) end
---@param IsDesignTime boolean
---@param UserId FString
function UWBP_Social_User_Entry_C:SetActionButtonInvites(IsDesignTime, UserId) end
---@param IsDesignTime boolean
function UWBP_Social_User_Entry_C:SetAllActionButtons(IsDesignTime) end
function UWBP_Social_User_Entry_C:SetSquadData() end
function UWBP_Social_User_Entry_C:BindToDelegates() end
function UWBP_Social_User_Entry_C:Construct() end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowChatMuteButton(isAllowed) end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowReportButton(isAllowed) end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowSocialButton(isAllowed) end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowSquadButton(isAllowed) end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowVoiceMuteButton(isAllowed) end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowBlockButton(isAllowed) end
---@param isAllowed boolean
function UWBP_Social_User_Entry_C:BP_CanShowInviteButtons(isAllowed) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_ChatMute_Btn_K2Node_ComponentBoundEvent_2_OnClicked__DelegateSignature(Button) end
---@param friendInfo FYOutpostFriendInfo
function UWBP_Social_User_Entry_C:BP_OnDataSet(friendInfo) end
function UWBP_Social_User_Entry_C:BP_SetToDefault() end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_Accept_Btn_Alt_K2Node_ComponentBoundEvent_8_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_Decline_Btn_Alt_K2Node_ComponentBoundEvent_9_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_Squad_Btn_Alt_K2Node_ComponentBoundEvent_10_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_SquadLeave_Btn_Alt_K2Node_ComponentBoundEvent_11_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_VoiceMute_Btn_Alt_K2Node_ComponentBoundEvent_12_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_Report_Btn_Alt_K2Node_ComponentBoundEvent_13_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Social_User_Entry_C:BndEvt__WBP_Social_User_Entry_WBP_Social_Btn_Alt_1_K2Node_ComponentBoundEvent_14_OnClicked__DelegateSignature(Button) end
---@param IsDesignTime boolean
function UWBP_Social_User_Entry_C:PreConstruct(IsDesignTime) end
---@param EntryPoint int32
function UWBP_Social_User_Entry_C:ExecuteUbergraph_WBP_Social_User_Entry(EntryPoint) end


