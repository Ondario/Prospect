---@meta

---@class UWBP_Chat_C : UUserWidget
---@field UberGraphFrame FPointerToUberGraphFrame
---@field ChatPanel_Anim UWidgetAnimation
---@field ChatPanelOverride USizeBox
---@field Gfx_Arrow UImage
---@field Gfx_DecoGradient UImage
---@field Gfx_Edge UImage
---@field Gfx_Fill UImage
---@field Gfx_Shadow UImage
---@field Gfx_Stroke UImage
---@field Icn_Chat UImage
---@field m_inputEditableTextBox UYEditableTextBox
---@field RichTxt_Message_01 URichTextBlock
---@field RichTxt_Message_02 URichTextBlock
---@field RichTxt_Message_03 URichTextBlock
---@field RichTxt_Message_04 URichTextBlock
---@field RichTxt_Message_05 URichTextBlock
---@field RichTxt_Message_06 URichTextBlock
---@field RichTxt_Message_07 URichTextBlock
---@field WBP_BlurBackerSimple_Panel UWBP_BlurBackerSimple_Panel_C
---@field WBP_Common_Empty_ShowHide UWBP_Common_Empty_Btn_C
---@field WBP_Deco_SelectionHighlight UWBP_SelectionHighlight_C
---@field WBP_Dummy_Btn UWBP_Dummy_Btn_C
---@field WBP_Footer_C_Panel UWBP_Footer_C_Panel_C
---@field WBP_HeaderSlim_Panel UWBP_HeaderSlim_Panel_C
---@field WBP_InputKey UWBP_InputKey_C
---@field WBP_SelectionBracket UWBP_SelectionBracket_C
---@field ShowChatPanel boolean
local UWBP_Chat_C = {}

---@param IsDesignTime boolean
function UWBP_Chat_C:PreConstruct(IsDesignTime) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Chat_C:BndEvt__WBP_Common_Empty_ShowHide_K2Node_ComponentBoundEvent_0_OnClicked__DelegateSignature(Button) end
---@param Button UWBP_ButtonBase_Btn_C
function UWBP_Chat_C:BndEvt__WBP_Dummy_Btn_K2Node_ComponentBoundEvent_1_OnClicked__DelegateSignature(Button) end
---@param EntryPoint int32
function UWBP_Chat_C:ExecuteUbergraph_WBP_Chat(EntryPoint) end


