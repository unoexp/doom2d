// 📁 01_Data/ScriptableObjects/Items/Material/MaterialItemSO.cs
// 材料物品数据定义
using UnityEngine;

/// <summary>
/// 材料物品定义：用于制作系统的原材料
/// 💡 新增材料只需创建.asset文件，无需改代码
/// </summary>
[CreateAssetMenu(fileName = "Item_Material_", menuName = "SurvivalGame/Items/Material")]
public class MaterialItemSO : ItemDefinitionSO
{
    [Header("材料属性")]
    public string MaterialTag; // 材料标签，用于制作配方匹配（如 "wood", "iron_ore"）
}
