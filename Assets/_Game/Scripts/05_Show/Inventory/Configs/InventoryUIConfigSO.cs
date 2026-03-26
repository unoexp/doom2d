// 📁 05_Show/Inventory/Configs/InventoryUIConfigSO.cs
// ⚠️ ScriptableObject配置，支持非代码修改

using System;
using UnityEngine;

/// <summary>
/// 背包UI配置ScriptableObject
/// 🏗️ 数据驱动设计：所有UI参数通过配置文件调整
/// 🚫 零运行时逻辑，纯数据容器
/// </summary>
[CreateAssetMenu(fileName = "InventoryUIConfig", menuName = "UI/Inventory/InventoryUIConfig")]
public class InventoryUIConfigSO : ScriptableObject
{
    [Header("📦 背包面板配置")]
    [SerializeField] private Vector2 _panelSize = new Vector2(800, 600);
    [SerializeField] private Vector2 _panelPosition = new Vector2(0, 0);
    [SerializeField] private Color _panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private Sprite _panelBackgroundSprite;

    [Header("🧱 槽位网格配置")]
    [SerializeField] private int _slotsPerRow = 6;
    [SerializeField] private int _totalRows = 4;
    [SerializeField] private Vector2 _slotSize = new Vector2(80, 80);
    [SerializeField] private Vector2 _slotSpacing = new Vector2(10, 10);
    [SerializeField] private Vector2 _gridPadding = new Vector2(20, 20);

    [Header("🎨 槽位视觉配置")]
    [SerializeField] private Color _emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color _normalSlotColor = Color.white;
    [SerializeField] private Color _highlightedColor = new Color(1, 0.9f, 0.5f, 1);
    [SerializeField] private Color _selectedColor = new Color(0.5f, 0.8f, 1, 0.5f);
    [SerializeField] private Color _overweightSlotColor = new Color(1, 0.3f, 0.3f, 0.5f);

    [SerializeField] private Sprite _slotBackgroundSprite;
    [SerializeField] private Sprite _slotSelectedSprite;
    [SerializeField] private Sprite _slotHighlightedSprite;

    [Header("⚡ 快捷栏配置")]
    [SerializeField] private int _quickSlotCount = 10;
    [SerializeField] private Vector2 _quickSlotSize = new Vector2(70, 70);
    [SerializeField] private Vector2 _quickSlotSpacing = new Vector2(5, 5);
    [SerializeField] private Vector2 _quickBarPosition = new Vector2(0, -300);

    [SerializeField] private Color _quickSlotNormalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color _quickSlotSelectedColor = new Color(0.5f, 0.8f, 1, 0.8f);

    [Header("🔄 动画配置")]
    [SerializeField] private float _panelOpenDuration = 0.3f;
    [SerializeField] private float _panelCloseDuration = 0.2f;
    [SerializeField] private AnimationCurve _panelOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve _panelCloseCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [SerializeField] private float _slotHoverDuration = 0.15f;
    [SerializeField] private float _slotClickDuration = 0.1f;
    [SerializeField] private AnimationCurve _slotHoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private float _dragAlpha = 0.7f;
    [SerializeField] private float _dragScale = 1.1f;
    [SerializeField] private bool _showDragGhost = true;

    [Header("📊 物品显示配置")]
    [SerializeField] private string _iconPathTemplate = "UI/Icons/{0}";
    [SerializeField] private Vector2 _itemIconSize = new Vector2(64, 64);
    [SerializeField] private Color _itemIconTint = Color.white;

    [SerializeField] private int _itemCountFontSize = 14;
    [SerializeField] private Color _itemCountColor = Color.white;
    [SerializeField] private Vector2 _itemCountOffset = new Vector2(2, -2);

    [SerializeField] private int _durabilityFontSize = 12;
    [SerializeField] private Color _durabilityNormalColor = Color.green;
    [SerializeField] private Color _durabilityWarningColor = Color.yellow;
    [SerializeField] private Color _durabilityCriticalColor = Color.red;

    [Header("💬 提示框配置")]
    [SerializeField] private Vector2 _tooltipSize = new Vector2(300, 200);
    [SerializeField] private Color _tooltipBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private float _tooltipShowDelay = 0.5f;
    [SerializeField] private float _tooltipFadeDuration = 0.2f;

    [Header("🔊 音效配置")]
    [SerializeField] private AudioClip _inventoryOpenSound;
    [SerializeField] private AudioClip _inventoryCloseSound;
    [SerializeField] private AudioClip _slotClickSound;
    [SerializeField] private AudioClip _itemPickupSound;
    [SerializeField] private AudioClip _itemDropSound;
    [SerializeField] private AudioClip _itemMoveSound;
    [SerializeField] private AudioClip _inventoryFullSound;

    [Header("⚙️ 性能配置")]
    [SerializeField] private int _initialPoolSize = 30;
    [SerializeField] private int _maxPoolSize = 100;
    [SerializeField] private bool _enableAsyncLoading = true;
    [SerializeField] private int _maxConcurrentLoads = 3;
    [SerializeField] private bool _enableVirtualization = true;
    [SerializeField] private int _visibleBufferSlots = 2;

    [Header("🌐 MOD支持配置")]
    [SerializeField] private bool _allowCustomSlotPrefabs = true;
    [SerializeField] private bool _allowCustomIcons = true;
    [SerializeField] private string _modSlotPrefabPath = "Mods/UI/Slots/";
    [SerializeField] private string _modIconPath = "Mods/UI/Icons/";

    // ============ 属性访问器 ============

    // 面板配置
    public Vector2 PanelSize => _panelSize;
    public Vector2 PanelPosition => _panelPosition;
    public Color PanelBackgroundColor => _panelBackgroundColor;
    public Sprite PanelBackgroundSprite => _panelBackgroundSprite;

    // 槽位网格
    public int SlotsPerRow => _slotsPerRow;
    public int TotalRows => _totalRows;
    public int TotalSlots => _slotsPerRow * _totalRows;
    public Vector2 SlotSize => _slotSize;
    public Vector2 SlotSpacing => _slotSpacing;
    public Vector2 GridPadding => _gridPadding;

    // 槽位视觉
    public Color EmptySlotColor => _emptySlotColor;
    public Color NormalSlotColor => _normalSlotColor;
    public Color HighlightedColor => _highlightedColor;
    public Color SelectedColor => _selectedColor;
    public Color OverweightSlotColor => _overweightSlotColor;

    public Sprite SlotBackgroundSprite => _slotBackgroundSprite;
    public Sprite SlotSelectedSprite => _slotSelectedSprite;
    public Sprite SlotHighlightedSprite => _slotHighlightedSprite;

    // 快捷栏
    public int QuickSlotCount => _quickSlotCount;
    public Vector2 QuickSlotSize => _quickSlotSize;
    public Vector2 QuickSlotSpacing => _quickSlotSpacing;
    public Vector2 QuickBarPosition => _quickBarPosition;
    public Color QuickSlotNormalColor => _quickSlotNormalColor;
    public Color QuickSlotSelectedColor => _quickSlotSelectedColor;

    // 动画
    public float PanelOpenDuration => _panelOpenDuration;
    public float PanelCloseDuration => _panelCloseDuration;
    public AnimationCurve PanelOpenCurve => _panelOpenCurve;
    public AnimationCurve PanelCloseCurve => _panelCloseCurve;

    public float SlotHoverDuration => _slotHoverDuration;
    public float SlotClickDuration => _slotClickDuration;
    public AnimationCurve SlotHoverCurve => _slotHoverCurve;

    public float DragAlpha => _dragAlpha;
    public float DragScale => _dragScale;
    public bool ShowDragGhost => _showDragGhost;

    // 物品显示
    public string IconPathTemplate => _iconPathTemplate;
    public Vector2 ItemIconSize => _itemIconSize;
    public Color ItemIconTint => _itemIconTint;

    public int ItemCountFontSize => _itemCountFontSize;
    public Color ItemCountColor => _itemCountColor;
    public Vector2 ItemCountOffset => _itemCountOffset;

    public int DurabilityFontSize => _durabilityFontSize;
    public Color DurabilityNormalColor => _durabilityNormalColor;
    public Color DurabilityWarningColor => _durabilityWarningColor;
    public Color DurabilityCriticalColor => _durabilityCriticalColor;

    // 提示框
    public Vector2 TooltipSize => _tooltipSize;
    public Color TooltipBackgroundColor => _tooltipBackgroundColor;
    public float TooltipShowDelay => _tooltipShowDelay;
    public float TooltipFadeDuration => _tooltipFadeDuration;

    // 音效
    public AudioClip InventoryOpenSound => _inventoryOpenSound;
    public AudioClip InventoryCloseSound => _inventoryCloseSound;
    public AudioClip SlotClickSound => _slotClickSound;
    public AudioClip ItemPickupSound => _itemPickupSound;
    public AudioClip ItemDropSound => _itemDropSound;
    public AudioClip ItemMoveSound => _itemMoveSound;
    public AudioClip InventoryFullSound => _inventoryFullSound;

    // 性能
    public int InitialPoolSize => _initialPoolSize;
    public int MaxPoolSize => _maxPoolSize;
    public bool EnableAsyncLoading => _enableAsyncLoading;
    public int MaxConcurrentLoads => _maxConcurrentLoads;
    public bool EnableVirtualization => _enableVirtualization;
    public int VisibleBufferSlots => _visibleBufferSlots;

    // MOD支持
    public bool AllowCustomSlotPrefabs => _allowCustomSlotPrefabs;
    public bool AllowCustomIcons => _allowCustomIcons;
    public string ModSlotPrefabPath => _modSlotPrefabPath;
    public string ModIconPath => _modIconPath;

    // ============ 工具方法 ============

    /// <summary>计算网格总尺寸</summary>
    public Vector2 CalculateGridSize()
    {
        float width = _slotsPerRow * _slotSize.x +
                     (_slotsPerRow - 1) * _slotSpacing.x +
                     _gridPadding.x * 2;

        float height = _totalRows * _slotSize.y +
                      (_totalRows - 1) * _slotSpacing.y +
                      _gridPadding.y * 2;

        return new Vector2(width, height);
    }

    /// <summary>计算槽位位置</summary>
    public Vector2 CalculateSlotPosition(int slotIndex)
    {
        int row = slotIndex / _slotsPerRow;
        int col = slotIndex % _slotsPerRow;

        float x = col * (_slotSize.x + _slotSpacing.x) + _gridPadding.x;
        float y = -row * (_slotSize.y + _slotSpacing.y) - _gridPadding.y;

        return new Vector2(x, y);
    }

    /// <summary>根据耐久度获取颜色</summary>
    public Color GetDurabilityColor(float durability)
    {
        if (durability >= 0.7f) return _durabilityNormalColor;
        if (durability >= 0.3f) return _durabilityWarningColor;
        return _durabilityCriticalColor;
    }

    /// <summary>检查槽位索引是否有效</summary>
    public bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < TotalSlots;
    }

    /// <summary>检查快捷栏索引是否有效</summary>
    public bool IsValidQuickSlotIndex(int index)
    {
        return index >= 0 && index < _quickSlotCount;
    }

    /// <summary>创建默认配置</summary>
    public static InventoryUIConfigSO CreateDefault()
    {
        var config = CreateInstance<InventoryUIConfigSO>();
        // 使用Inspector中设置的默认值
        return config;
    }
}

/// <summary>编辑器扩展：配置验证</summary>
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(InventoryUIConfigSO))]
public class InventoryUIConfigSOEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var config = target as InventoryUIConfigSO;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("配置预览", UnityEditor.EditorStyles.boldLabel);

        UnityEditor.EditorGUILayout.LabelField($"总槽位数: {config.TotalSlots}");
        UnityEditor.EditorGUILayout.LabelField($"快捷栏数: {config.QuickSlotCount}");

        var gridSize = config.CalculateGridSize();
        UnityEditor.EditorGUILayout.LabelField($"网格尺寸: {gridSize.x:F0} x {gridSize.y:F0}");

        // 验证配置
        if (config.TotalSlots <= 0)
        {
            UnityEditor.EditorGUILayout.HelpBox("槽位数必须大于0", UnityEditor.MessageType.Error);
        }

        if (config.QuickSlotCount <= 0)
        {
            UnityEditor.EditorGUILayout.HelpBox("快捷栏数必须大于0", UnityEditor.MessageType.Error);
        }
    }
}
#endif