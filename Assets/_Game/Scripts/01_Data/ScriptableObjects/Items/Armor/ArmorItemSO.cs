// 📁 01_Data/ScriptableObjects/Items/Armor/ArmorItemSO.cs
// 护甲物品数据定义
using UnityEngine;

/// <summary>
/// 装备部位枚举
/// </summary>
public enum ArmorSlot
{
    Head,       // 头部
    Chest,      // 胸部
    Legs,       // 腿部
    Feet        // 脚部
}

/// <summary>
/// 护甲物品定义：防御装备
/// 💡 新增护甲只需创建.asset文件，无需改代码
/// </summary>
[CreateAssetMenu(fileName = "Item_Armor_", menuName = "SurvivalGame/Items/Armor")]
public class ArmorItemSO : ItemDefinitionSO
{
    [Header("护甲属性")]
    public float Defense = 5f;
    public ArmorSlot EquipSlot = ArmorSlot.Chest;
}
