// 📁 02_Base/Interfaces/IInventorySystem.cs
// 背包系统接口定义，供表现层通过ServiceLocator访问

using SurvivalGame.Data.Inventory;

/// <summary>
/// 背包系统接口，表现层通过此接口与业务层通信
/// 🏗️ 定义在02_Base层，03_Core实现，05_Show引用
/// </summary>
public interface IInventorySystem
{
    InventoryData[] GetInventoryData();
    QuickSlotData[] GetQuickSlotData();
    void MoveItem(SlotType sourceType, int sourceIndex, SlotType targetType, int targetIndex);
    void SelectQuickSlot(int slotIndex);
    WeightInfo GetWeightInfo();
    int GetTotalItemCount(string itemId);
    bool TryAddItem(string itemId, int quantity);
    bool TryRemoveItem(string itemId, int quantity);
    int SelectedQuickAccessSlot { get; }
}

/// <summary>背包槽位数据（UI用）</summary>
public struct InventoryData
{
    public int SlotIndex;
    public string ItemId;
    public int Amount;
    public float Durability;
}

/// <summary>快捷栏槽位数据（UI用）</summary>
public struct QuickSlotData
{
    public int SlotIndex;
    public string ItemId;
    public int Amount;
    public float Durability;
}

/// <summary>负重信息</summary>
public struct WeightInfo
{
    public float CurrentWeight;
    public float MaxWeight;
}
