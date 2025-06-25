---@meta

---@class UWBP_SocialOverview_C : UYWidget_SocialOverview
---@field UberGraphFrame FPointerToUberGraphFrame
---@field ScreenIn_Anim UWidgetAnimation
---@field ContentSwitcher UWidgetSwitcher
---@field EditTxtSingle_UserName UEditableTextBox
---@field Friends_ScrollBox UScrollBox
---@field Search_ScrollBox UScrollBox
---@field SocialUsersList_WBP_Blocked UWBP_SocialUserList_C
---@field SocialUsersList_WBP_FriendInvites UWBP_SocialUserList_C
---@field SocialUsersList_WBP_Offline UWBP_SocialUserList_C
---@field SocialUsersList_WBP_Online UWBP_SocialUserList_C
---@field SocialUsersList_WBP_SearchResults UWBP_SocialUserList_C
---@field SocialUsersList_WBP_SquadInvites UWBP_SocialUserList_C
---@field VerticalBox_Search_Initial UVerticalBox
---@field VerticalBox_Search_NoMatchFound UVerticalBox
---@field WBP_BlurFullScreenBacker_Panel UWBP_BlurFullScreenBacker_Panel_C
---@field WBP_Close_Btn UWBP_Common_Btn_C
---@field WBP_DividerHorizontal_Panel UWBP_DividerHorizontal_Panel_C
---@field WBP_Search_Btn UWBP_Common_Simple_Btn_C
---@field WBP_SearchClear_Btn UWBP_Common_Simple_Btn_C
---@field WBP_TabNavigation UWBP_TabNavigation_C
---@field displayLimitWarning boolean
local UWBP_SocialOverview_C = {}

function UWBP_SocialOverview_C:ShowFriendLimitReached() end
function UWBP_SocialOverview_C:SetTooltips() end
---@return boolean
function UWBP_SocialOverview_C:BP_HandleBackKey() end
---@param newVisibility ESlateVisibility
---@return UWidgetAnimation
function UWBP_SocialOverview_C:BP_AnimateVisibility(newVisibility) end
---@param userIds TArray<FString>
function UWBP_SocialOverview_C:OnFriendSearchResult(userIds) end
---@param hasFound boolean
---@param foundUser FYOutpostFriendInfo
function UWBP_SocialOverview_C:OnUserSearchResult(hasFound, foundUser) end
function UWBP_SocialOverview_C:ResetContentDataForSearchResults() end
function UWBP_SocialOverview_C:ResetContentDataForFriends() end
function UWBP_SocialOverview_C:ResetContentData() end
---@param Index int32
---@param TabElement UWBP_TabElementBase_TabElem_C
function UWBP_SocialOverview_C:BndEvt__WBP_SocialOverview_WBP_TabNavigation_K2Node_ComponentBoundEvent_0_OnTabChanged__DelegateSignature(Index, TabElement) end
function UWBP_SocialOverview_C:BP_OnWidgetHidden() end
---@param hasFound boolean
---@param foundUser FYOutpostFriendInfo
function UWBP_SocialOverview_C:BP_OnUserSearchResult(hasFound, foundUser) end
---@param Text FText
---@param CommitMethod ETextCommit
function UWBP_SocialOverview_C:BndEvt__WBP_SocialOverview_EditTxtSingle_UserName_K2Node_ComponentBoundEvent_3_OnEditableTextBoxCommittedEvent__DelegateSignature(Text, CommitMethod) end
---@param foundUserIds TArray<FString>
function UWBP_SocialOverview_C:BP_OnFriendSearchResponse(foundUserIds) end
function UWBP_SocialOverview_C:BP_OnWidgetShown() end
---@param Text FText
function UWBP_SocialOverview_C:BndEvt__WBP_SocialOverview_EditTxtSingle_UserName_K2Node_ComponentBoundEvent_6_OnEditableTextBoxChangedEvent__DelegateSignature(Text) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_SocialOverview_C:BndEvt__WBP_SocialOverview_WBP_Close_Btn_K2Node_ComponentBoundEvent_2_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_SocialOverview_C:BndEvt__WBP_SocialOverview_WBP_SearchClear_Btn1_K2Node_ComponentBoundEvent_5_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_SocialOverview_C:BndEvt__WBP_SocialOverview_WBP_Search_Btn1_K2Node_ComponentBoundEvent_7_OnClicked__DelegateSignature(Button) end
function UWBP_SocialOverview_C:OnSocialPressed() end
---@param EntryPoint int32
function UWBP_SocialOverview_C:ExecuteUbergraph_WBP_SocialOverview(EntryPoint) end


