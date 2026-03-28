// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Inventory/Tooltip/ItemTooltipView.cs
// 物品详情Tooltip的View层。负责渲染物品信息面板。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 物品Tooltip View。
///
/// 核心职责：
///   · 显示物品名称、图标、描述、属性等信息
///   · 根据稀有度设置名称颜色
///   · 跟随鼠标/手指位置定位
///   · 绑定 ViewModel 事件驱动显示
///
/// 设计说明：
///   · 不继承 UIPanel（非栈式管理，由 Presenter 直接控制显示）
///   · 使用 CanvasGroup 控制透明度和交互
///   · 不阻挡射线（blocksRaycasts = false）
/// </summary>
public class ItemTooltipView : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // UI 引用
    // ══════════════════════════════════════════════════════

    [Header("基础信息")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _categoryText;
    [SerializeField] private Image _iconImage;

    [Header("描述")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("属性")]
    [SerializeField] private TextMeshProUGUI _weightText;
    [SerializeField] private TextMeshProUGUI _stackText;
    [SerializeField] private TextMeshProUGUI _extraLinesText;

    [Header("耐久度")]
    [SerializeField] private GameObject _durabilityGroup;
    [SerializeField] private Slider _durabilityBar;
    [SerializeField] private TextMeshProUGUI _durabilityText;

    [Header("配置")]
    [SerializeField] private Vector2 _offset = new Vector2(20f, -20f);

    // ══════════════════════════════════════════════════════
    // 组件
    // ══════════════════════════════════════════════════════

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Canvas _parentCanvas;

    // ══════════════════════════════════════════════════════
    // ViewModel
    // ══════════════════════════════════════════════════════

    private ItemTooltipViewModel _viewModel;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rectTransform = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();

        SetVisible(false);
    }

    private void Update()
    {
        if (_viewModel != null && _viewModel.IsVisible)
        {
            UpdatePosition();
        }
    }

    private void OnDestroy()
    {
        Bind(null);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>绑定 ViewModel</summary>
    public void Bind(ItemTooltipViewModel viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.OnShow -= OnShowTooltip;
            _viewModel.OnHide -= OnHideTooltip;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.OnShow += OnShowTooltip;
            _viewModel.OnHide += OnHideTooltip;
        }
    }

    // ══════════════════════════════════════════════════════
    // ViewModel 事件回调
    // ══════════════════════════════════════════════════════

    private void OnShowTooltip(ItemTooltipData data)
    {
        // 名称（稀有度颜色）
        if (_nameText != null)
        {
            _nameText.text = data.DisplayName;
            _nameText.color = ItemTooltipViewModel.GetRarityColor(data.Rarity);
        }

        // 分类
        if (_categoryText != null)
        {
            _categoryText.text = ItemTooltipViewModel.GetCategoryText(data.Category);
        }

        // 图标
        if (_iconImage != null)
        {
            _iconImage.sprite = data.Icon;
            _iconImage.enabled = data.Icon != null;
        }

        // 描述
        if (_descriptionText != null)
        {
            _descriptionText.text = data.Description;
        }

        // 重量
        if (_weightText != null)
        {
            _weightText.text = $"重量: {data.Weight:F1}";
        }

        // 堆叠
        if (_stackText != null)
        {
            _stackText.text = data.MaxStackSize > 1
                ? $"数量: {data.StackSize}/{data.MaxStackSize}"
                : string.Empty;
        }

        // 耐久度
        if (_durabilityGroup != null)
        {
            bool showDurability = data.HasDurability;
            _durabilityGroup.SetActive(showDurability);

            if (showDurability)
            {
                if (_durabilityBar != null)
                    _durabilityBar.value = data.MaxDurability > 0f
                        ? data.CurrentDurability / data.MaxDurability
                        : 0f;

                if (_durabilityText != null)
                    _durabilityText.text = $"耐久: {data.CurrentDurability:F0}/{data.MaxDurability:F0}";
            }
        }

        // 额外属性行
        if (_extraLinesText != null)
        {
            if (data.ExtraLines != null && data.ExtraLines.Length > 0)
            {
                _extraLinesText.text = string.Join("\n", data.ExtraLines);
                _extraLinesText.gameObject.SetActive(true);
            }
            else
            {
                _extraLinesText.gameObject.SetActive(false);
            }
        }

        SetVisible(true);
        UpdatePosition();
    }

    private void OnHideTooltip()
    {
        SetVisible(false);
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = false; // Tooltip 不阻挡射线
        _canvasGroup.interactable = false;
    }

    /// <summary>跟随鼠标位置，带边界检测</summary>
    private void UpdatePosition()
    {
        if (_rectTransform == null || _parentCanvas == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvas.transform as RectTransform,
            Input.mousePosition,
            _parentCanvas.worldCamera,
            out localPoint);

        localPoint += _offset;

        // 简单边界检测：防止超出屏幕
        var canvasRect = _parentCanvas.transform as RectTransform;
        if (canvasRect != null)
        {
            var size = _rectTransform.sizeDelta;
            var canvasSize = canvasRect.sizeDelta;
            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;

            if (localPoint.x + size.x > halfW)
                localPoint.x = halfW - size.x;
            if (localPoint.y - size.y < -halfH)
                localPoint.y = -halfH + size.y;
        }

        _rectTransform.anchoredPosition = localPoint;
    }
}
