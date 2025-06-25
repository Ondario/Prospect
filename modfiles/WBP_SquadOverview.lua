---@meta

---@class UWBP_SquadOverview_C : UYWidget_OutpostSquadToggle
---@field UberGraphFrame FPointerToUberGraphFrame
---@field Btn_LeaveSquad UButton
---@field Gfx_Divider UImage
local UWBP_SquadOverview_C = {}

---@return UWidget
function UWBP_SquadOverview_C:Get_LeaveSquadButton_ToolTipWidget() end
function UWBP_SquadOverview_C:BndEvt__WBP_SquadOverview_Btn_LeaveSquad_K2Node_ComponentBoundEvent_0_OnButtonClickedEvent__DelegateSignature() end
---@param IsInSquad boolean
function UWBP_SquadOverview_C:BP_SquadUpdate(IsInSquad) end
---@param EntryPoint int32
function UWBP_SquadOverview_C:ExecuteUbergraph_WBP_SquadOverview(EntryPoint) end


