// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Commands/CraftCommand.cs
// 制作命令。封装制作操作的执行与撤销逻辑。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 可撤销的制作命令。
///
/// 核心职责：
///   · Execute: 调用 CraftingSystem.Craft 消耗材料并产出物品
///   · Undo: 归还消耗的材料并移除产出的物品
///
/// 设计说明：
///   · 执行时记录实际消耗的材料快照，用于精确撤销
///   · 通过 ServiceLocator 获取 CraftingSystem 和 IInventorySystem
///   · 撤销后重新发布背包更新事件保持 UI 同步
/// </summary>
public class CraftCommand : ICommand
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly string _recipeId;
    private readonly bool _nearWorkbench;

    /// <summary>制作是否成功执行过</summary>
    private bool _executed;

    /// <summary>产出的物品ID和数量（撤销时移除）</summary>
    private string _outputItemId;
    private int _outputAmount;

    /// <summary>消耗的材料快照（撤销时归还）</summary>
    private CraftingIngredient[] _consumedIngredients;

    // ══════════════════════════════════════════════════════
    // ICommand
    // ══════════════════════════════════════════════════════

    public string Description { get; private set; }

    // ══════════════════════════════════════════════════════
    // 构造
    // ══════════════════════════════════════════════════════

    /// <param name="recipeId">配方ID</param>
    /// <param name="nearWorkbench">是否在工作台附近</param>
    public CraftCommand(string recipeId, bool nearWorkbench = false)
    {
        _recipeId = recipeId;
        _nearWorkbench = nearWorkbench;
        Description = $"制作 {recipeId}";
    }

    // ══════════════════════════════════════════════════════
    // 执行
    // ══════════════════════════════════════════════════════

    public void Execute()
    {
        var craftingSystem = ServiceLocator.Get<CraftingSystem>();
        if (craftingSystem == null)
        {
            Debug.LogWarning("[CraftCommand] CraftingSystem 未注册");
            return;
        }

        // 记录配方数据用于撤销
        var recipe = craftingSystem.GetRecipe(_recipeId);
        if (recipe == null)
        {
            Debug.LogWarning($"[CraftCommand] 配方不存在: {_recipeId}");
            return;
        }

        _consumedIngredients = recipe.Ingredients;
        _outputItemId = recipe.OutputItem != null ? recipe.OutputItem.ItemId : string.Empty;
        _outputAmount = recipe.OutputAmount;
        Description = $"制作 {recipe.DisplayName} x{_outputAmount}";

        var result = craftingSystem.Craft(_recipeId, _nearWorkbench);
        _executed = result == CraftingResult.Success;
    }

    public void Undo()
    {
        if (!_executed) return;

        var inventory = ServiceLocator.Get<IInventorySystem>();
        if (inventory == null) return;

        // 移除产出物品
        if (!string.IsNullOrEmpty(_outputItemId))
        {
            inventory.TryRemoveItem(_outputItemId, _outputAmount);
        }

        // 归还消耗的材料
        if (_consumedIngredients != null)
        {
            for (int i = 0; i < _consumedIngredients.Length; i++)
            {
                var ingredient = _consumedIngredients[i];
                if (ingredient.Item == null) continue;
                inventory.TryAddItem(ingredient.Item.ItemId, ingredient.Amount);
            }
        }

        _executed = false;
        Debug.Log($"[CraftCommand] 已撤销制作: {Description}");
    }
}
