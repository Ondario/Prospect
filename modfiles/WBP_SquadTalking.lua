---@meta

---@class UWBP_SquadTalking_C : UYWidget_TalkingIcon
---@field UberGraphFrame FPointerToUberGraphFrame
---@field Gfx_Circle UImage
---@field GraphicHolder UCanvasPanel
---@field Icn_Talking UImage
---@field ShowBacker boolean
local UWBP_SquadTalking_C = {}

---@param IsDesignTime boolean
function UWBP_SquadTalking_C:PreConstruct(IsDesignTime) end
---@param IsTalking boolean
function UWBP_SquadTalking_C:BP_SetTalking(IsTalking) end
---@param EntryPoint int32
function UWBP_SquadTalking_C:ExecuteUbergraph_WBP_SquadTalking(EntryPoint) end


