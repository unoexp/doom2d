// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/SaveData/PlayerSaveData.cs
// 玩家存档数据结构。纯数据，可序列化。
// 注意：使用 List 而非 Dictionary，因为 JsonUtility 不支持序列化 Dictionary。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

/// <summary>
/// 玩家存档数据。包含位置、装备、状态等。
/// </summary>
[Serializable]
public class PlayerSaveData
{
    /// <summary>玩家世界坐标 X</summary>
    public float PositionX;

    /// <summary>玩家世界坐标 Y</summary>
    public float PositionY;

    /// <summary>玩家朝向（true=面向右）</summary>
    public bool FacingRight = true;

    /// <summary>当前所在场景名称</summary>
    public string CurrentScene;

    /// <summary>装备槽位数据</summary>
    public List<EquipmentSlotEntry> EquippedItems = new List<EquipmentSlotEntry>();

    /// <summary>已解锁的配方ID列表</summary>
    public List<string> UnlockedRecipeIds = new List<string>();

    /// <summary>游戏内经过的总时间（秒）</summary>
    public float TotalPlayTime;
}

/// <summary>
/// 装备槽位存档条目。
/// </summary>
[Serializable]
public struct EquipmentSlotEntry
{
    public EquipmentSlot Slot;
    public string ItemId;
    public float Durability;

    public EquipmentSlotEntry(EquipmentSlot slot, string itemId, float durability)
    {
        Slot = slot;
        ItemId = itemId;
        Durability = durability;
    }
}
