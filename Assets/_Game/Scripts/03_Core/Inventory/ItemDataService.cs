// 📁 03_Core/Inventory/ItemDataService.cs
// 物品数据服务，提供物品定义的加载和缓存
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Core.Inventory
{
    /// <summary>
    /// 物品数据服务，通过Resources加载和缓存ItemDefinitionSO
    /// 🏗️ 架构说明：核心业务层服务，向ServiceLocator注册
    /// </summary>
    public class ItemDataService : MonoBehaviour, IItemDataService
    {
        [Header("配置")]
        [SerializeField] private string _itemsFolderPath = "Items/";

        private Dictionary<string, ItemDefinitionSO> _cache = new Dictionary<string, ItemDefinitionSO>();
        private bool _allLoaded = false;

        private void Awake()
        {
            ServiceLocator.Register<IItemDataService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IItemDataService>();
        }

        /// <summary>通过ItemId获取物品定义</summary>
        public ItemDefinitionSO GetItemDefinition(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            // 先查缓存
            if (_cache.TryGetValue(itemId, out var cached))
                return cached;

            // 如果尚未全量加载，尝试单独加载
            if (!_allLoaded)
            {
                var definition = Resources.Load<ItemDefinitionSO>($"{_itemsFolderPath}{itemId}");
                if (definition != null)
                {
                    _cache[itemId] = definition;
                    return definition;
                }

                // 单独加载失败，尝试全量加载一次
                LoadAllItems();
                if (_cache.TryGetValue(itemId, out cached))
                    return cached;
            }

            return null;
        }

        /// <summary>全量加载所有物品定义</summary>
        private void LoadAllItems()
        {
            if (_allLoaded) return;

            var allItems = Resources.LoadAll<ItemDefinitionSO>(_itemsFolderPath);
            foreach (var item in allItems)
            {
                if (!string.IsNullOrEmpty(item.ItemId))
                {
                    _cache[item.ItemId] = item;
                }
            }

            _allLoaded = true;
        }
    }
}
