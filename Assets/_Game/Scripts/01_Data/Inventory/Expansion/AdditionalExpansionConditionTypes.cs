// 📁 01_Data/Inventory/Expansion/AdditionalExpansionConditionTypes.cs
// [架构迁移说明] 具体条件实现类已移至：
//   03_Core/Inventory/Expansion/Conditions/ExpansionConditionImplementations.cs
// 移动原因：条件实现含运行时逻辑（Random 占位符、未来 ServiceLocator 调用），属于 03_Core 职责

namespace SurvivalGame.Data.Inventory.Expansion
{
    // QuestCompletionCondition、CurrencyConsumptionCondition、CompositeExpansionCondition、TimeWindowCondition
    // 已移至 SurvivalGame.Core.Inventory.Expansion 命名空间
}
