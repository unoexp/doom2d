// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/SaveData/InventorySaveData.cs
// 背包存档数据结构。纯数据，可序列化。
// 注意：使用 List 而非 Dictionary，因为 JsonUtility 不支持序列化 Dictionary。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

/// <summary>
/// 背包存档数据。包含所有容器的物品数据。
/// </summary>
[Serializable]
public class InventorySaveData
{
    /// <summary>所有容器的存档数据</summary>
    public List<ContainerSaveEntry> Containers = new List<ContainerSaveEntry>();

    /// <summary>当前金币数</summary>
    public int Gold;
}

/// <summary>
/// 单个容器存档条目。
/// </summary>
[Serializable]
public class ContainerSaveEntry
{
    /// <summary>容器ID（主背包/快捷栏等）</summary>
    public string ContainerId;

    /// <summary>容器容量</summary>
    public int Capacity;

    /// <summary>槽位数据列表</summary>
    public List<SlotSaveEntry> Slots = new List<SlotSaveEntry>();
}

/// <summary>
/// 单个槽位存档条目。
/// </summary>
[Serializable]
public struct SlotSaveEntry
{
    /// <summary>槽位索引</summary>
    public int SlotIndex;

    /// <summary>物品ID（空槽位为空字符串）</summary>
    public string ItemId;

    /// <summary>堆叠数量</summary>
    public int Amount;

    /// <summary>当前耐久度（-1=无耐久度）</summary>
    public float Durability;

    public SlotSaveEntry(int slotIndex, string itemId, int amount, float durability)
    {
        SlotIndex = slotIndex;
        ItemId = itemId;
        Amount = amount;
        Durability = durability;
    }
}
