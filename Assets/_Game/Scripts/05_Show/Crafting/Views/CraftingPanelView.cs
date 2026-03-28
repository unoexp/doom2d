// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Crafting/Views/CraftingPanelView.cs
// 制作界面面板View。继承UIPanel，纯显示组件。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 制作界面面板View。
///
/// 职责：
///   · 渲染配方列表
///   · 显示选中配方的详情（材料需求、产出信息）
///   · 接收用户交互（选择配方、点击制作按钮）
///   · 不直接访问业务系统，通过事件通知 Presenter
/// </summary>
public class CraftingPanelView : UIPanel
{
    // ══════════════════════════════════════════════════════
    // UI引用
    // ══════════════════════════════════════════════════════

    [Header("配方列表")]
    [SerializeField] private Transform _recipeListContainer;
    [SerializeField] private GameObject _recipeItemPrefab;

    [Header("配方详情")]
    [SerializeField] private TextMeshProUGUI _recipeNameText;
    [SerializeField] private TextMeshProUGUI _recipeDescText;
    [SerializeField] private Image _outputIcon;
    [SerializeField] private TextMeshProUGUI _outputAmountText;
    [SerializeField] private Transform _ingredientListContainer;
    [SerializeField] private GameObject _ingredientItemPrefab;

    [Header("操作按钮")]
    [SerializeField] private Button _craftButton;
    [SerializeField] private TextMeshProUGUI _craftButtonText;

    [Header("反馈")]
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private float _resultDisplayDuration = 2f;

    // ══════════════════════════════════════════════════════
    // 事件（Presenter订阅）
    // ══════════════════════════════════════════════════════

    /// <summary>用户选中配方</summary>
    public event Action<int> OnRecipeSelected;

    /// <summary>用户点击制作按钮</summary>
    public event Action OnCraftClicked;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private CraftingViewModel _viewModel;
    private float _resultTimer;
    private readonly List<GameObject> _recipeItemInstances = new List<GameObject>();
    private readonly List<GameObject> _ingredientInstances = new List<GameObject>();

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>绑定 ViewModel</summary>
    public void Bind(CraftingViewModel viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.OnRecipeListUpdated -= HandleRecipeListUpdated;
            _viewModel.OnSelectedRecipeChanged -= HandleSelectedRecipeChanged;
            _viewModel.OnCraftingResult -= HandleCraftingResult;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.OnRecipeListUpdated += HandleRecipeListUpdated;
            _viewModel.OnSelectedRecipeChanged += HandleSelectedRecipeChanged;
            _viewModel.OnCraftingResult += HandleCraftingResult;
        }
    }

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();

        if (_craftButton != null)
        {
            _craftButton.onClick.AddListener(() => OnCraftClicked?.Invoke());
        }

        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_resultTimer > 0f)
        {
            _resultTimer -= Time.unscaledDeltaTime;
            if (_resultTimer <= 0f && _resultText != null)
            {
                _resultText.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        Bind(null);
    }

    // ══════════════════════════════════════════════════════
    // ViewModel 事件处理
    // ══════════════════════════════════════════════════════

    /// <summary>配方列表更新 → 重建列表UI</summary>
    private void HandleRecipeListUpdated(List<RecipeDisplayData> recipes)
    {
        ClearInstances(_recipeItemInstances);

        if (_recipeListContainer == null || _recipeItemPrefab == null) return;

        for (int i = 0; i < recipes.Count; i++)
        {
            var instance = Instantiate(_recipeItemPrefab, _recipeListContainer);
            _recipeItemInstances.Add(instance);

            // 设置名称文本
            var nameText = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = recipes[i].DisplayName;
                // 不可制作的配方显示灰色
                nameText.color = recipes[i].CanCraft ? Color.white : Color.gray;
            }

            // 设置点击事件
            int index = i; // 闭包捕获
            var button = instance.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnRecipeSelected?.Invoke(index));
            }
        }
    }

    /// <summary>选中配方变化 → 更新详情面板</summary>
    private void HandleSelectedRecipeChanged(RecipeDisplayData recipe)
    {
        // 更新配方名称和描述
        if (_recipeNameText != null) _recipeNameText.text = recipe.DisplayName;
        if (_recipeDescText != null) _recipeDescText.text = recipe.Description;

        // 更新产出信息
        if (_outputAmountText != null) _outputAmountText.text = $"×{recipe.OutputAmount}";

        // 更新制作按钮状态
        if (_craftButton != null) _craftButton.interactable = recipe.CanCraft;
        if (_craftButtonText != null)
        {
            _craftButtonText.text = recipe.CanCraft ? "制作" : "材料不足";
        }

        // 更新材料列表
        UpdateIngredientList(recipe.Ingredients);
    }

    /// <summary>制作结果反馈</summary>
    private void HandleCraftingResult(CraftingResult result, string recipeName)
    {
        if (_resultText == null) return;

        switch (result)
        {
            case CraftingResult.Success:
                _resultText.text = $"制作成功：{recipeName}";
                _resultText.color = Color.green;
                break;
            case CraftingResult.Failed_NoMaterial:
                _resultText.text = "材料不足";
                _resultText.color = Color.red;
                break;
            case CraftingResult.Failed_Overloaded:
                _resultText.text = "背包已满";
                _resultText.color = Color.yellow;
                break;
            default:
                _resultText.text = "制作失败";
                _resultText.color = Color.red;
                break;
        }

        _resultText.gameObject.SetActive(true);
        _resultTimer = _resultDisplayDuration;
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>更新材料需求列表</summary>
    private void UpdateIngredientList(IngredientDisplayData[] ingredients)
    {
        ClearInstances(_ingredientInstances);

        if (_ingredientListContainer == null || _ingredientItemPrefab == null) return;
        if (ingredients == null) return;

        for (int i = 0; i < ingredients.Length; i++)
        {
            var instance = Instantiate(_ingredientItemPrefab, _ingredientListContainer);
            _ingredientInstances.Add(instance);

            var text = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                var ing = ingredients[i];
                text.text = $"{ing.DisplayName}  {ing.CurrentAmount}/{ing.RequiredAmount}";
                text.color = ing.IsSatisfied ? Color.white : Color.red;
            }
        }
    }

    /// <summary>清理实例化的UI对象</summary>
    private void ClearInstances(List<GameObject> instances)
    {
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] != null)
                Destroy(instances[i]);
        }
        instances.Clear();
    }
}
