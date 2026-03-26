// 📁 01_Data/ScriptableObjects/Items/Weapon/WeaponItemSO.cs
// 武器物品数据定义
using UnityEngine;

/// <summary>
/// 武器物品定义：近战/远程武器
/// 💡 新增武器只需创建.asset文件，无需改代码
/// </summary>
[CreateAssetMenu(fileName = "Item_Weapon_", menuName = "SurvivalGame/Items/Weapon")]
public class WeaponItemSO : ItemDefinitionSO
{
    [Header("武器属性")]
    public float AttackDamage = 10f;
    public float AttackSpeed = 1f;
    public float AttackRange = 1.5f;
}
