// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Crafting/Presenters/CraftingPresenter.cs
// 制作界面Presenter。连接业务事件与ViewModel，处理用户交互。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 制作界面 Presenter。
///
/// 核心职责：
///   · 从 CraftingSystem 获取配方数据，转换为 ViewModel 数据
///   · 订阅 EventBus 事件（背包变化→刷新可制作状态、制作结果→反馈）
///   · 处理 View 用户交互（选择配方、点击制作）→ 调用业务系统
///
/// 设计说明：
///   · 遵循 Presenter → ViewModel → View 单向数据流
///   · 通过 ServiceLocator 获取 CraftingSystem 和 IInventorySystem
///   · 不直接操作 View 组件
/// </summary>
public class CraftingPresenter : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 引用
    // ══════════════════════════════════════════════════════

    [SerializeField] private CraftingPanelView _panelView;

    private CraftingViewModel _viewModel;
    private CraftingSystem _craftingSystem;
    private IInventorySystem _inventorySystem;
    private IItemDataService _itemDataService;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _viewModel = new CraftingViewModel();
    }

    private void Start()
    {
        _craftingSystem = ServiceLocator.Get<CraftingSystem>();
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();
        _itemDataService = ServiceLocator.Get<IItemDataService>();

        // 绑定 View ↔ ViewModel
        if (_panelView != null)
        {
            _panelView.Bind(_viewModel);
            _panelView.OnRecipeSelected += HandleRecipeSelected;
            _panelView.OnCraftClicked += HandleCraftClicked;

            // 注册到 UIManager
            var uiManager = ServiceLocator.Get<UIManager>();
            if (uiManager != null)
            {
                uiManager.RegisterPanel(_panelView);
            }
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CraftingResultEvent>(OnCraftingResult);
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CraftingResultEvent>(OnCraftingResult);
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    private void OnDestroy()
    {
        if (_panelView != null)
        {
            _panelView.OnRecipeSelected -= HandleRecipeSelected;
            _panelView.OnCraftClicked -= HandleCraftClicked;
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>打开制作界面</summary>
    public void OpenCraftingPanel()
    {
        RefreshRecipeList();
        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null && _panelView != null)
        {
            uiManager.OpenPanel(_panelView);
        }
    }

    /// <summary>关闭制作界面</summary>
    public void CloseCraftingPanel()
    {
        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null && _panelView != null)
        {
            uiManager.ClosePanel(_panelView);
        }
    }

    // ══════════════════════════════════════════════════════
    // 数据转换
    // ══════════════════════════════════════════════════════

    /// <summary>刷新配方列表</summary>
    private void RefreshRecipeList()
    {
        if (_craftingSystem == null) return;

        var recipes = _craftingSystem.GetUnlockedRecipes();
        var displayList = new List<RecipeDisplayData>(recipes.Count);

        for (int i = 0; i < recipes.Count; i++)
        {
            displayList.Add(ConvertToDisplayData(recipes[i]));
        }

        _viewModel.SetRecipes(displayList);
    }

    /// <summary>将配方数据转换为UI展示数据</summary>
    private RecipeDisplayData ConvertToDisplayData(RecipeDefinitionSO recipe)
    {
        var data = new RecipeDisplayData
        {
            RecipeId = recipe.RecipeId,
            DisplayName = recipe.DisplayName,
            Description = recipe.Description,
            OutputItemId = recipe.OutputItem != null ? recipe.OutputItem.ItemId : "",
            OutputAmount = recipe.OutputAmount,
            RequiresWorkbench = recipe.RequiresWorkbench,
            CanCraft = _craftingSystem.Validate(recipe.RecipeId) == CraftingResult.Success,
        };

        // 转换材料需求
        if (recipe.Ingredients != null)
        {
            data.Ingredients = new IngredientDisplayData[recipe.Ingredients.Length];
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                var ing = recipe.Ingredients[i];
                string itemId = ing.Item != null ? ing.Item.ItemId : "";
                int currentAmount = 0;

                if (_inventorySystem != null && !string.IsNullOrEmpty(itemId))
                {
                    currentAmount = _inventorySystem.GetTotalItemCount(itemId);
                }

                data.Ingredients[i] = new IngredientDisplayData
                {
                    ItemId = itemId,
                    DisplayName = ing.Item != null ? ing.Item.DisplayName : "未知",
                    RequiredAmount = ing.Amount,
                    CurrentAmount = currentAmount,
                    IsSatisfied = currentAmount >= ing.Amount
                };
            }
        }

        return data;
    }

    // ══════════════════════════════════════════════════════
    // 用户交互处理
    // ══════════════════════════════════════════════════════

    private void HandleRecipeSelected(int index)
    {
        _viewModel.SelectRecipe(index);
    }

    private void HandleCraftClicked()
    {
        var selected = _viewModel.SelectedRecipe;
        if (!selected.HasValue) return;

        // 通过 EventBus 发起制作请求（CraftingSystem 监听处理）
        EventBus.Publish(new CraftingRequestEvent
        {
            RecipeId = selected.Value.RecipeId,
            Amount = 1
        });
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    /// <summary>制作结果 → 通知 ViewModel 显示反馈并刷新列表</summary>
    private void OnCraftingResult(CraftingResultEvent evt)
    {
        var recipe = _craftingSystem != null ? _craftingSystem.GetRecipe(evt.RecipeId) : null;
        string name = recipe != null ? recipe.DisplayName : evt.RecipeId;

        _viewModel.NotifyCraftingResult(evt.Result, name);

        // 制作成功后刷新列表（材料数量变化）
        if (evt.Result == CraftingResult.Success)
        {
            RefreshRecipeList();
        }
    }

    /// <summary>背包变化 → 刷新可制作状态</summary>
    private void OnInventoryChanged(InventoryChangedEvent evt)
    {
        // 仅在面板可见时刷新
        if (_panelView != null && _panelView.IsVisible)
        {
            RefreshRecipeList();
        }
    }
}
