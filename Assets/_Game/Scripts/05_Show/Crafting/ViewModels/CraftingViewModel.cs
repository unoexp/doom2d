// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Crafting/ViewModels/CraftingViewModel.cs
// 制作界面的ViewModel，持有配方列表和制作状态的UI数据。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

/// <summary>
/// 单条配方的UI展示数据
/// </summary>
public struct RecipeDisplayData
{
    /// <summary>配方ID</summary>
    public string RecipeId;
    /// <summary>显示名称</summary>
    public string DisplayName;
    /// <summary>描述</summary>
    public string Description;
    /// <summary>产出物品ID</summary>
    public string OutputItemId;
    /// <summary>产出数量</summary>
    public int OutputAmount;
    /// <summary>是否可制作（材料足够）</summary>
    public bool CanCraft;
    /// <summary>是否需要工作台</summary>
    public bool RequiresWorkbench;
    /// <summary>所需材料列表</summary>
    public IngredientDisplayData[] Ingredients;
}

/// <summary>
/// 单条材料需求的UI展示数据
/// </summary>
public struct IngredientDisplayData
{
    /// <summary>材料物品ID</summary>
    public string ItemId;
    /// <summary>材料显示名称</summary>
    public string DisplayName;
    /// <summary>需要数量</summary>
    public int RequiredAmount;
    /// <summary>当前持有数量</summary>
    public int CurrentAmount;
    /// <summary>是否满足</summary>
    public bool IsSatisfied;
}

/// <summary>
/// 制作界面的ViewModel。
///
/// 纯C#类，持有配方列表和当前选中配方的状态。
/// Presenter 写入数据时触发事件通知 View 更新。
/// </summary>
public class CraftingViewModel
{
    // ══════════════════════════════════════════════════════
    // 事件（View订阅）
    // ══════════════════════════════════════════════════════

    /// <summary>配方列表刷新</summary>
    public event Action<List<RecipeDisplayData>> OnRecipeListUpdated;

    /// <summary>选中配方变化</summary>
    public event Action<RecipeDisplayData> OnSelectedRecipeChanged;

    /// <summary>制作结果反馈</summary>
    public event Action<CraftingResult, string> OnCraftingResult;

    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    private readonly List<RecipeDisplayData> _recipes = new List<RecipeDisplayData>();
    private int _selectedIndex = -1;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public int SelectedIndex => _selectedIndex;
    public int RecipeCount => _recipes.Count;

    public RecipeDisplayData? SelectedRecipe =>
        _selectedIndex >= 0 && _selectedIndex < _recipes.Count
            ? _recipes[_selectedIndex]
            : (RecipeDisplayData?)null;

    // ══════════════════════════════════════════════════════
    // 公有 API（Presenter调用）
    // ══════════════════════════════════════════════════════

    /// <summary>设置配方列表</summary>
    public void SetRecipes(List<RecipeDisplayData> recipes)
    {
        _recipes.Clear();
        _recipes.AddRange(recipes);
        _selectedIndex = _recipes.Count > 0 ? 0 : -1;

        OnRecipeListUpdated?.Invoke(_recipes);

        if (_selectedIndex >= 0)
        {
            OnSelectedRecipeChanged?.Invoke(_recipes[_selectedIndex]);
        }
    }

    /// <summary>选中指定配方</summary>
    public void SelectRecipe(int index)
    {
        if (index < 0 || index >= _recipes.Count) return;
        _selectedIndex = index;
        OnSelectedRecipeChanged?.Invoke(_recipes[_selectedIndex]);
    }

    /// <summary>通知制作结果</summary>
    public void NotifyCraftingResult(CraftingResult result, string recipeName)
    {
        OnCraftingResult?.Invoke(result, recipeName);
    }

    /// <summary>更新单条配方的可制作状态</summary>
    public void UpdateRecipeCraftability(int index, bool canCraft)
    {
        if (index < 0 || index >= _recipes.Count) return;
        var data = _recipes[index];
        data.CanCraft = canCraft;
        _recipes[index] = data;
    }
}
