# 预制体结构设计

## 1. 预制体架构层次

```
📁 Prefabs/
├── InventoryPanel.prefab          (主背包面板)
│   ├── Canvas
│   ├── EventSystem
│   └── InventoryRoot (RectTransform)
│       ├── Background (Image)
│       ├── PanelHeader (UI组件)
│       │   ├── TitleText
│       │   ├── CloseButton
│       │   └── FilterDropdown
│       ├── SlotContainer (GridLayoutGroup)
│       │   ├── Slot_0 (InventorySlot.prefab)
│       │   ├── Slot_1
│       │   └── ...Slot_23
│       ├── QuickSlotContainer
│       │   ├── QuickSlot_0 (QuickSlot.prefab)
│       │   └── ...QuickSlot_9
│       └── FooterPanel
│           ├── WeightText
│           ├── GoldText
│           └── SortButton
├── InventorySlot.prefab           (可复用槽位)
│   ├── SlotRoot (RectTransform)
│   ├── SlotBackground (Image)
│   ├── ItemIcon (Image) - 默认隐藏
│   ├── ItemCount (Text) - 默认隐藏
│   ├── SelectionIndicator - 默认隐藏
│   └── DragVisual (CanvasGroup) - 拖拽时显示
├── QuickSlot.prefab              (快捷栏槽位)
│   ├── QuickSlotRoot
│   ├── Background
│   ├── ItemIcon
│   ├── KeybindText (显示快捷键: 1-0)
│   ├── SelectionIndicator
│   └── CooldownOverlay (Image, FillAmount控制)
└── ItemTooltip.prefab            (物品提示)
    ├── TooltipCanvas (最高渲染层级)
    ├── Background (9-slice精灵)
    ├── ItemNameText
    ├── ItemTypeText
    ├── ItemDescriptionText
    ├── StatsContainer
    │   ├── DurabilityText
    │   └── WeightText
    └── ActionButtons (使用/丢弃)
```

## 2. 组件拆分策略

### 逻辑/视觉分离
- **Logic Prefab**: 只包含脚本和必要的事件触发器
- **Visual Prefab**: 只包含UI元素和动画控制器
- **运行时组合**: 通过`Addressables`或`Resources`动态加载视觉资源

### 槽位预制体配置
```csharp
// InventorySlot预制体的Inspector配置示例
[CreateAssetMenu(fileName = "SlotConfig", menuName = "UI/Inventory/SlotConfig")]
public class InventorySlotConfigSO : ScriptableObject
{
    [Header("尺寸设置")]
    public Vector2 SlotSize = new Vector2(80, 80);
    public Vector2 SlotSpacing = new Vector2(10, 10);

    [Header("颜色配置")]
    public Color EmptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    public Color NormalSlotColor = Color.white;
    public Color HighlightedColor = new Color(1, 0.9f, 0.5f, 1);
    public Color SelectedColor = new Color(0.5f, 0.8f, 1, 0.5f);

    [Header("动画配置")]
    public float HoverAnimationDuration = 0.15f;
    public float ClickAnimationDuration = 0.1f;
    public AnimationCurve HoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("拖拽配置")]
    public float DragAlpha = 0.7f;
    public float DragScale = 1.1f;
    public bool ShowDragGhost = true;
}
```

## 3. 预制体实例化策略

### 对象池管理 (UIPoolManager.cs)
```csharp
// 槽位对象池
private class SlotPool : MonoBehaviour
{
    private Queue<GameObject> _pooledSlots = new Queue<GameObject>();
    private GameObject _slotPrefab;
    private Transform _poolParent;

    public void Initialize(GameObject prefab, int initialSize)
    {
        _slotPrefab = prefab;
        _poolParent = new GameObject("SlotPool").transform;
        _poolParent.SetParent(transform);

        // 预热对象池
        for (int i = 0; i < initialSize; i++)
        {
            var slot = Instantiate(_slotPrefab, _poolParent);
            slot.SetActive(false);
            _pooledSlots.Enqueue(slot);
        }
    }

    public GameObject GetSlot()
    {
        if (_pooledSlots.Count > 0)
        {
            var slot = _pooledSlots.Dequeue();
            slot.SetActive(true);
            return slot;
        }

        // 池为空，创建新实例
        return Instantiate(_slotPrefab);
    }

    public void ReturnSlot(GameObject slot)
    {
        slot.SetActive(false);
        slot.transform.SetParent(_poolParent);
        _pooledSlots.Enqueue(slot);
    }
}
```

### 异步加载策略
1. **优先加载策略**: 先加载可见区域的槽位（虚拟化支持）
2. **渐近式加载**: 使用`Addressables.LoadAssetAsync`分帧加载
3. **缓存策略**: 常用图标保持内存缓存，不常用图标LRU淘汰

## 4. 预制体依赖配置

### Addressable资产分组
```
📁 AddressableGroups/
├── UI_Inventory_Core (关键路径)
│   ├── InventoryPanel.prefab
│   ├── InventorySlot.prefab
│   └── InventoryUIManager.prefab
├── UI_Inventory_Visuals (视觉资源)
│   ├── Icons/ (所有物品图标)
│   ├── Backgrounds/ (面板背景)
│   └── Effects/ (UI特效)
└── UI_Inventory_Sounds (音效)
    ├── OpenCloseSound.asset
    ├── SlotClickSound.asset
    └── ItemMoveSound.asset
```

### 资源引用模式
```csharp
// 使用ScriptableObject配置资源引用
[CreateAssetMenu(fileName = "UIAssetConfig", menuName = "UI/Inventory/AssetConfig")]
public class UIAssetConfigSO : ScriptableObject
{
    // Addressable键
    [Header("预制体引用")]
    public string InventoryPanelKey = "UI_Inventory_Core/InventoryPanel";
    public string InventorySlotKey = "UI_Inventory_Core/InventorySlot";
    public string QuickSlotKey = "UI_Inventory_Core/QuickSlot";

    [Header("图标引用模板")]
    public string IconPathTemplate = "UI/Icons/{0}";

    [Header("音效引用")]
    public AudioClip OpenSound;
    public AudioClip CloseSound;
    public AudioClip ItemPickupSound;
    public AudioClip ItemDropSound;

    // 异步加载方法
    public async UniTask<GameObject> LoadInventoryPanelAsync()
    {
        return await Addressables.LoadAssetAsync<GameObject>(InventoryPanelKey);
    }
}
```

## 5. MOD扩展支持

### MOD预制体覆盖机制
```csharp
public interface IInventoryUIProvider
{
    // MOD可以提供自定义预制体
    GameObject GetCustomSlotPrefab();
    GameObject GetCustomPanelPrefab();

    // 或者只覆盖视觉资源
    Sprite GetSlotBackground(string slotType);
    Sprite GetItemIconOverride(string itemId);

    // 配置覆盖
    InventorySlotConfigSO GetSlotConfigOverride();
}

// 默认实现
public class DefaultInventoryUIProvider : IInventoryUIProvider
{
    public GameObject GetCustomSlotPrefab() => null; // 返回null使用默认
    // ...其他方法
}
```