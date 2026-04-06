// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Crafting/CraftingSystem.cs
// 制作系统。管理配方注册、材料验证、制作执行。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央制作管理系统。
///
/// 核心职责：
///   · 管理所有已注册配方（通过 ScriptableObject 数据驱动）
///   · 验证制作条件（材料、工作台、解锁状态）
///   · 执行制作流程（消耗材料、产出物品）
///   · 管理配方解锁状态
///   · 通过 EventBus 广播制作结果
///
/// 设计说明：
///   · 配方数据在 Inspector 中通过 RecipeDefinitionSO 数组配置
///   · 运行时通过 IInventorySystem 查询/消耗材料
///   · 制作结果通过 CraftingResultEvent 广播，UI 层订阅显示
/// </summary>
public class CraftingSystem : MonoBehaviour, ICraftingSystem
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("配方数据")]
    [SerializeField] private RecipeDefinitionSO[] _recipes;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>RecipeId → 配方定义（快速查找）</summary>
    private readonly Dictionary<string, RecipeDefinitionSO> _recipeMap
        = new Dictionary<string, RecipeDefinitionSO>();

    /// <summary>已解锁的配方ID集合</summary>
    private readonly HashSet<string> _unlockedRecipes = new HashSet<string>();

    /// <summary>背包系统引用（Start 中获取）</summary>
    private IInventorySystem _inventorySystem;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<CraftingSystem>(this);
        ServiceLocator.Register<ICraftingSystem>(this);

        // 构建配方查找表
        if (_recipes != null)
        {
            for (int i = 0; i < _recipes.Length; i++)
            {
                var recipe = _recipes[i];
                if (recipe == null || string.IsNullOrEmpty(recipe.RecipeId)) continue;
                _recipeMap[recipe.RecipeId] = recipe;

                // 默认解锁的配方
                if (recipe.UnlockedByDefault)
                    _unlockedRecipes.Add(recipe.RecipeId);
            }
        }
    }

    private void Start()
    {
        // Start 中获取，确保其他系统已在 Awake 中注册
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CraftingRequestEvent>(OnCraftingRequest);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CraftingRequestEvent>(OnCraftingRequest);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<CraftingSystem>();
        ServiceLocator.Unregister<ICraftingSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 获取所有已注册的配方。
    /// </summary>
    public RecipeDefinitionSO[] GetAllRecipes()
    {
        return _recipes ?? Array.Empty<RecipeDefinitionSO>();
    }

    /// <summary>
    /// 获取所有已解锁的配方。
    /// </summary>
    public List<RecipeDefinitionSO> GetUnlockedRecipes()
    {
        var result = new List<RecipeDefinitionSO>();
        foreach (var id in _unlockedRecipes)
        {
            if (_recipeMap.TryGetValue(id, out var recipe))
                result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// 根据 ID 获取配方。
    /// </summary>
    public RecipeDefinitionSO GetRecipe(string recipeId)
    {
        _recipeMap.TryGetValue(recipeId, out var recipe);
        return recipe;
    }

    /// <summary>
    /// 检查配方是否已解锁。
    /// </summary>
    public bool IsUnlocked(string recipeId)
    {
        return _unlockedRecipes.Contains(recipeId);
    }

    /// <summary>
    /// 验证是否满足制作条件（不执行制作）。
    /// </summary>
    public CraftingResult Validate(string recipeId, bool nearWorkbench = false)
    {
        return CraftingValidator.Validate(recipeId, _recipeMap, _unlockedRecipes,
                                          _inventorySystem, nearWorkbench);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 制作与解锁
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 执行制作。验证条件 → 消耗材料 → 产出物品。
    /// </summary>
    /// <param name="recipeId">配方ID</param>
    /// <param name="nearWorkbench">是否在工作台附近</param>
    /// <returns>制作结果</returns>
    public CraftingResult Craft(string recipeId, bool nearWorkbench = false)
    {
        // 验证
        var result = Validate(recipeId, nearWorkbench);
        if (result != CraftingResult.Success)
        {
            PublishResult(recipeId, result, null, 0);
            return result;
        }

        var recipe = _recipeMap[recipeId];

        // 消耗材料
        for (int i = 0; i < recipe.Ingredients.Length; i++)
        {
            var ingredient = recipe.Ingredients[i];
            if (ingredient.Item == null) continue;
            _inventorySystem.TryRemoveItem(ingredient.Item.ItemId, ingredient.Amount);
        }

        // 产出物品 — 通过 EventBus 通知背包系统添加
        // 注意：实际添加物品由 InventorySystem 处理 ItemAddedToInventoryEvent
        PublishResult(recipeId, CraftingResult.Success,
                      recipe.OutputItem.ItemId, recipe.OutputAmount);

        Debug.Log($"[CraftingSystem] 制作成功：{recipe.DisplayName} x{recipe.OutputAmount}");
        return CraftingResult.Success;
    }

    /// <summary>
    /// 解锁一个配方。
    /// </summary>
    public void UnlockRecipe(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return;
        if (!_recipeMap.ContainsKey(recipeId))
        {
            Debug.LogWarning($"[CraftingSystem] 尝试解锁不存在的配方：{recipeId}");
            return;
        }

        if (_unlockedRecipes.Add(recipeId))
        {
            EventBus.Publish(new RecipeUnlockedEvent { RecipeId = recipeId });
            Debug.Log($"[CraftingSystem] 配方已解锁：{recipeId}");
        }
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnCraftingRequest(CraftingRequestEvent evt)
    {
        for (int i = 0; i < evt.Amount; i++)
        {
            var result = Craft(evt.RecipeId, nearWorkbench: false);
            if (result != CraftingResult.Success) break;
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void PublishResult(string recipeId, CraftingResult result,
                               string outputItemId, int outputAmount)
    {
        EventBus.Publish(new CraftingResultEvent
        {
            RecipeId = recipeId,
            Result = result,
            OutputItemId = outputItemId ?? string.Empty,
            OutputAmount = outputAmount
        });
    }
}
