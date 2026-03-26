// 📁 01_Data/ScriptableObjects/Items/Tool/ToolItemSO.cs
// 工具物品数据定义
using UnityEngine;

/// <summary>
/// 工具类型枚举
/// </summary>
public enum ToolType
{
    Axe,        // 斧头（砍伐）
    Pickaxe,    // 镐（采矿）
    Shovel,     // 铲（挖掘）
    Hoe,        // 锄头（耕作）
    FishingRod  // 鱼竿（钓鱼）
}

/// <summary>
/// 工具物品定义：采集/建造用工具
/// 💡 新增工具只需创建.asset文件，无需改代码
/// </summary>
[CreateAssetMenu(fileName = "Item_Tool_", menuName = "SurvivalGame/Items/Tool")]
public class ToolItemSO : ItemDefinitionSO
{
    [Header("工具属性")]
    public ToolType ToolType = ToolType.Axe;
    public float GatherEfficiency = 1f;
}
