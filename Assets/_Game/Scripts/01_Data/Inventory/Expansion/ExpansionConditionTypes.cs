// 📁 01_Data/Inventory/Expansion/ExpansionConditionTypes.cs
// [架构迁移说明] 具体条件实现类已移至：
//   03_Core/Inventory/Expansion/Conditions/ExpansionConditionImplementations.cs
// 移动原因：条件实现需要通过 ServiceLocator 访问运行时服务，属于 03_Core 业务层职责
// 此文件保留命名空间以维护已序列化资产的类型引用兼容性

namespace SurvivalGame.Data.Inventory.Expansion
{
    // 具体条件实现类（ResourceConsumptionCondition、SkillRequirementCondition 等）
    // 已移至 SurvivalGame.Core.Inventory.Expansion 命名空间
}
