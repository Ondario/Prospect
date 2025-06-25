---@meta

---@class UWBP_FullMessage_C : UYWidget
---@field UberGraphFrame FPointerToUberGraphFrame
---@field WidgetInAndFadeOut_Anim UWidgetAnimation
---@field WidgetIn_Anim UWidgetAnimation
---@field Gfx_ColorFeedback UImage
---@field Gfx_Shadow UImage
---@field Gfx_SlantSecondary UImage
---@field Txt_Copy UTextBlock
---@field Txt_PrimaryMessage UTextBlock
---@field Txt_SecondaryMessage UTextBlock
---@field Message FText
---@field bPlayAnimation boolean
---@field IsSecondary boolean
local UWBP_FullMessage_C = {}

---@param newVisibility ESlateVisibility
---@return UWidgetAnimation
function UWBP_FullMessage_C:BP_AnimateVisibility(newVisibility) end
---@param IsDesignTime boolean
function UWBP_FullMessage_C:PreConstruct(IsDesignTime) end
function UWBP_FullMessage_C:Construct() end
function UWBP_FullMessage_C:BagFullNotificationFadeInAndOut() end
---@param EntryPoint int32
function UWBP_FullMessage_C:ExecuteUbergraph_WBP_FullMessage(EntryPoint) end


