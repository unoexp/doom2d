// 📁 05_Show/Inventory/Adapters/InventoryViewModelAdapter.cs
// ViewModel和数据层之间的转换器
using System;
using System.Collections.Generic;
using RootsAndRuin.Data.Inventory;
using RootsAndRuin.Core.Inventory;

namespace RootsAndRuin.Show.Inventory.Adapters
{
    /// <summary>
    /// ViewModel和数据层之间的转换器
    /// 🏗️ 职责：将业务层的数据结构转换为ViewModel需要的格式
    /// ⚠️ 纯转换逻辑，无业务逻辑，无Unity依赖
    /// </summary>
    public static class InventoryViewModelAdapter
    {
        // ============ 数据转换方法 ============

        /// <summary>将ItemStack转换为ViewModel可用的数据</summary>
        public static SlotData ConvertToSlotData(ItemStack itemStack, int slotIndex, SlotType slotType, string keybind = "")
        {
            var data = new SlotData
            {
                SlotIndex = slotIndex,
                SlotType = slotType,
                Keybind = keybind,
                ItemId = itemStack.ItemId,
                ItemAmount = itemStack.Quantity,
                ItemDurability = itemStack.Durability,
                CustomDataJson = itemStack.CustomDataJson
            };

            return data;
        }

        /// <summary>将InventoryContainer转换为SlotData集合</summary>
        public static List<SlotData> ConvertContainerToSlotData(InventoryContainer container, SlotType slotType)
        {
            var slotDataList = new List<SlotData>();

            if (!container.IsValid) return slotDataList;

            for (int i = 0; i < container.Capacity; i++)
            {
                var slot = container.Slots[i];
                string keybind = slotType == SlotType.QuickAccess ? GetQuickAccessKeybind(i) : "";

                slotDataList.Add(new SlotData
                {
                    SlotIndex = i,
                    SlotType = slotType,
                    Keybind = keybind,
                    ItemId = slot.ItemStack.ItemId,
                    ItemAmount = slot.ItemStack.Quantity,
                    ItemDurability = slot.ItemStack.Durability,
                    CustomDataJson = slot.ItemStack.CustomDataJson,
                    SlotDefinition = slot // 保留原始槽位定义用于验证
                });
            }

            return slotDataList;
        }

        /// <summary>将InventorySystem状态转换为ViewModel状态</summary>
        public static InventoryViewModelState ConvertSystemToViewModelState(
            InventorySystem system, bool isInventoryOpen = false)
        {
            if (system == null)
                return new InventoryViewModelState();

            var state = new InventoryViewModelState
            {
                IsInventoryOpen = isInventoryOpen,
                SelectedQuickAccessSlot = system.SelectedQuickAccessSlot,
                MainInventorySlots = ConvertContainerToSlotData(system.MainInventory, SlotType.General),
                QuickAccessSlots = ConvertContainerToSlotData(system.QuickAccess, SlotType.QuickAccess),
                // 计算总重量（需要遍历所有物品）
                TotalWeight = CalculateTotalWeight(system),
                GoldAmount = 0 // TODO: 从游戏状态获取金币数量
            };

            return state;
        }

        /// <summary>将UI事件转换为业务层可用的数据</summary>
        public static InventoryOperationData ConvertUIEventToOperationData(SlotDragEndedEvent dragEvent, SlotData sourceSlotData)
        {
            return new InventoryOperationData
            {
                OperationType = InventoryOperationType.MoveItem,
                SourceContainerId = GetContainerIdFromSlotType(sourceSlotData.SlotType),
                SourceSlotIndex = dragEvent.SourceSlotIndex,
                TargetContainerId = dragEvent.TargetSlotIndex >= 0
                    ? GetContainerIdFromSlotType(sourceSlotData.SlotType) // 暂时假设同一容器
                    : "", // 无效目标
                TargetSlotIndex = dragEvent.TargetSlotIndex,
                ItemId = sourceSlotData.ItemId,
                Quantity = sourceSlotData.ItemAmount
            };
        }

        // ============ 计算辅助方法 ============

        /// <summary>计算背包总重量</summary>
        private static float CalculateTotalWeight(InventorySystem system)
        {
            float totalWeight = 0f;

            // 计算主背包重量
            foreach (var slot in system.MainInventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    var definition = slot.ItemStack.GetDefinition();
                    if (definition != null)
                    {
                        totalWeight += definition.Weight * slot.ItemStack.Quantity;
                    }
                }
            }

            // 计算快捷栏重量
            foreach (var slot in system.QuickAccess.Slots)
            {
                if (!slot.IsEmpty)
                {
                    var definition = slot.ItemStack.GetDefinition();
                    if (definition != null)
                    {
                        totalWeight += definition.Weight * slot.ItemStack.Quantity;
                    }
                }
            }

            return totalWeight;
        }

        /// <summary>获取快捷栏快捷键文本</summary>
        private static string GetQuickAccessKeybind(int slotIndex)
        {
            return slotIndex switch
            {
                0 => "1",
                1 => "2",
                2 => "3",
                3 => "4",
                4 => "5",
                5 => "6",
                6 => "7",
                7 => "8",
                8 => "9",
                9 => "0",
                _ => ""
            };
        }

        /// <summary>根据槽位类型获取容器ID</summary>
        private static string GetContainerIdFromSlotType(SlotType slotType)
        {
            return slotType switch
            {
                SlotType.General => "MainInventory",
                SlotType.QuickAccess => "QuickAccess",
                SlotType.Weapon => "Equipment_Weapon",
                SlotType.Armor => "Equipment_Armor",
                SlotType.Tool => "Equipment_Tool",
                _ => $"MOD_{slotType}" // MOD扩展槽位
            };
        }

        // ============ 内部数据结构 ============

        /// <summary>槽位数据，ViewModel的原始数据源</summary>
        public struct SlotData
        {
            public int SlotIndex;
            public SlotType SlotType;
            public string Keybind;
            public string ItemId;
            public int ItemAmount;
            public float ItemDurability;
            public string CustomDataJson;
            public InventorySlot SlotDefinition; // 原始槽位定义
        }

        /// <summary>背包ViewModel的完整状态</summary>
        public struct InventoryViewModelState
        {
            public bool IsInventoryOpen;
            public int SelectedQuickAccessSlot;
            public List<SlotData> MainInventorySlots;
            public List<SlotData> QuickAccessSlots;
            public float TotalWeight;
            public int GoldAmount;
            public string CurrentFilter;
            public string CurrentSortMethod;
        }

        /// <summary>库存操作数据，用于UI到业务层的通信</summary>
        public struct InventoryOperationData
        {
            public InventoryOperationType OperationType;
            public string SourceContainerId;
            public int SourceSlotIndex;
            public string TargetContainerId;
            public int TargetSlotIndex;
            public string ItemId;
            public int Quantity;
            public Vector2? DropPosition;
        }

        /// <summary>库存操作类型</summary>
        public enum InventoryOperationType
        {
            MoveItem,
            UseItem,
            DropItem,
            SplitStack,
            MergeStack
        }

        /// <summary>简化版Vector2结构（避免Unity依赖）</summary>
        public struct Vector2
        {
            public float X;
            public float Y;

            public Vector2(float x, float y)
            {
                X = x;
                Y = y;
            }
        }
    }
}